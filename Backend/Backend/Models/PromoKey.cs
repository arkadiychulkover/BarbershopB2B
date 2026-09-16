using Backend.Models.Enums;

namespace Backend.Models
{
    public class PromoKey
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Криптографический хеш ключа с солью (не открытый ключ)
        /// </summary>
        public string KeyHash { get; set; } = string.Empty;

        /// <summary>
        /// Случайная криптографическая соль (Base64)
        /// </summary>
        public string Salt { get; set; } = string.Empty;

        /// <summary>
        /// Количество дней подписки, предоставляемых данным ключом
        /// </summary>
        public int Days { get; set; }

        /// <summary>
        /// Статус ключа (Created, Used, Deleted)
        /// </summary>
        public PromoKeyStatus Status { get; set; } = PromoKeyStatus.Created;

        /// <summary>
        /// Дата создания ключа
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Дата активации ключа
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// Id владельца заведения, активировавшего данный ключ
        /// </summary>
        public Guid? UsedByOwnerId { get; set; }

        /// <summary>
        /// Заведение / владелец, активировавший ключ
        /// </summary>
        public BarbershopOwner? UsedByOwner { get; set; }
    }
}
