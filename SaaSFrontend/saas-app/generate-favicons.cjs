const fs = require('fs');
const path = require('path');
const { chromium } = require('@playwright/test');

/**
 * Builds a standard Windows ICO file buffer from an array of PNG buffers.
 * Each entry: { width, height, buffer }
 */
function createIco(images) {
  const count = images.length;
  const headerSize = 6;
  const dirEntrySize = 16;
  const totalHeaderSize = headerSize + count * dirEntrySize;

  let currentOffset = totalHeaderSize;
  const entries = [];

  for (const img of images) {
    entries.push({
      width: img.width >= 256 ? 0 : img.width,
      height: img.height >= 256 ? 0 : img.height,
      colorCount: 0,
      reserved: 0,
      planes: 1,
      bitCount: 32,
      bytesInRes: img.buffer.length,
      imageOffset: currentOffset
    });
    currentOffset += img.buffer.length;
  }

  const outBuffer = Buffer.alloc(currentOffset);

  // ICONDIR Header
  outBuffer.writeUInt16LE(0, 0); // Reserved
  outBuffer.writeUInt16LE(1, 2); // 1 = ICO
  outBuffer.writeUInt16LE(count, 4); // Number of images

  // ICONDIRENTRY list
  let dirOffset = headerSize;
  for (let i = 0; i < count; i++) {
    const e = entries[i];
    outBuffer.writeUInt8(e.width, dirOffset + 0);
    outBuffer.writeUInt8(e.height, dirOffset + 1);
    outBuffer.writeUInt8(e.colorCount, dirOffset + 2);
    outBuffer.writeUInt8(e.reserved, dirOffset + 3);
    outBuffer.writeUInt16LE(e.planes, dirOffset + 4);
    outBuffer.writeUInt16LE(e.bitCount, dirOffset + 6);
    outBuffer.writeUInt32LE(e.bytesInRes, dirOffset + 8);
    outBuffer.writeUInt32LE(e.imageOffset, dirOffset + 12);
    dirOffset += dirEntrySize;
  }

  // Raw PNG image data
  for (let i = 0; i < count; i++) {
    images[i].buffer.copy(outBuffer, entries[i].imageOffset);
  }

  return outBuffer;
}

async function generate() {
  const publicDir = path.resolve(__dirname, 'public');
  const svgPath = path.join(publicDir, 'favicon.svg');
  const svgContent = fs.readFileSync(svgPath, 'utf8');

  console.log('Launching headless Chromium to render SVG favicons...');
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  const sizes = [
    { name: 'favicon-16x16.png', size: 16 },
    { name: 'favicon-32x32.png', size: 32 },
    { name: 'favicon-48x48.png', size: 48 },
    { name: 'apple-touch-icon.png', size: 180 },
    { name: 'android-chrome-192x192.png', size: 192 },
    { name: 'android-chrome-512x512.png', size: 512 }
  ];

  const htmlTemplate = (size) => `
    <!DOCTYPE html>
    <html>
      <head>
        <style>
          * { margin: 0; padding: 0; box-sizing: border-box; }
          body { width: ${size}px; height: ${size}px; overflow: hidden; background: transparent; }
          svg { width: 100%; height: 100%; display: block; }
        </style>
      </head>
      <body>
        ${svgContent}
      </body>
    </html>
  `;

  const renderedBuffers = {};

  for (const { name, size } of sizes) {
    await page.setViewportSize({ width: size, height: size });
    await page.setContent(htmlTemplate(size));
    const buf = await page.screenshot({ omitBackground: true });
    fs.writeFileSync(path.join(publicDir, name), buf);
    renderedBuffers[size] = buf;
    console.log(`✓ Generated: ${name} (${size}x${size})`);
  }

  await browser.close();

  // Create multi-resolution favicon.ico (16, 32, 48)
  const icoImages = [
    { width: 16, height: 16, buffer: renderedBuffers[16] },
    { width: 32, height: 32, buffer: renderedBuffers[32] },
    { width: 48, height: 48, buffer: renderedBuffers[48] }
  ];

  const icoBuffer = createIco(icoImages);
  fs.writeFileSync(path.join(publicDir, 'favicon.ico'), icoBuffer);
  console.log(`✓ Generated: favicon.ico (16x16, 32x32, 48x48)`);

  console.log('All favicons generated successfully in /public!');
}

generate().catch(err => {
  console.error('Favicon generation error:', err);
  process.exit(1);
});
