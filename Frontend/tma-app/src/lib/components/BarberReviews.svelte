<script lang="ts">
  import { onMount } from 'svelte';
  import { apiFetch } from '../api';
  import Icon from './Icon.svelte';
  import { m } from '../paraglide/messages.js';
  import { currentLocale } from '../locale';
  
  let reviews = [];
  let loading = true;
  let error = null;

  onMount(async () => {
    loading = true;
    try {
      reviews = await apiFetch('/api/Barber/my-reviews');
    } catch (e) {
      console.error(e);
      error = m.tma_reviews_load_error();
    } finally {
      loading = false;
    }
  });
</script>

<div class="reviews-container">
  {#if loading}
    <div class="status-msg">{m.tma_reviews_loading()}</div>
  {:else if error}
    <div class="status-msg error">{error}</div>
  {:else if reviews.length === 0}
    <div class="empty-state">
      <div class="icon">
        <Icon name="star" size={36} color="var(--pastel-amber)" />
      </div>
      <p>{m.tma_reviews_empty()}</p>
    </div>
  {:else}
    <div class="reviews-list">
      {#each reviews as review}
        <div class="review-card">
          <div class="review-header">
            <span class="client-name">{review.clientName}</span>
            <span class="review-date">{new Date(review.createdAt).toLocaleDateString($currentLocale === 'ru' ? 'ru-RU' : 'en-US')}</span>
          </div>
          <div class="review-rating">
            {#each [1, 2, 3, 4, 5] as star}
              <Icon 
                name={star <= review.rating ? 'star' : 'star-outline'} 
                size={16} 
                color="var(--pastel-amber)" 
              />
            {/each}
          </div>
          {#if review.comment}
            <div class="review-comment">{review.comment}</div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  .reviews-container {
    height: 100%;
    overflow-y: auto;
    padding: 4px 0 30px;
    animation: fadeIn 0.3s var(--ease-spring);
  }
  
  .status-msg, .empty-state {
    text-align: center;
    padding: 50px 20px;
    color: var(--text-secondary);
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
  }
  
  .error {
    color: var(--pastel-coral);
  }
  
  .empty-state .icon {
    font-size: 40px;
    margin-bottom: 12px;
  }
  
  .reviews-list {
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  
  .review-card {
    background: var(--bg-surface);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    border-radius: var(--radius-lg);
    padding: 18px 20px;
    box-shadow: var(--shadow-glass);
    border: 1px solid var(--border-subtle);
    transition: border-color 0.2s;
  }

  .review-card:hover {
    border-color: var(--border-glass);
  }
  
  .review-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 8px;
  }
  
  .client-name {
    font-weight: 700;
    font-size: 15px;
    color: var(--text-primary);
  }
  
  .review-date {
    font-size: 12px;
    color: var(--text-muted);
    font-variant-numeric: tabular-nums;
  }
  
  .review-rating {
    color: var(--pastel-amber);
    font-size: 16px;
    margin-bottom: 8px;
    letter-spacing: 3px;
    text-shadow: 0 0 10px var(--pastel-amber-glow);
  }
  
  .review-comment {
    font-size: 14px;
    color: var(--text-secondary);
    line-height: 1.5;
    background: var(--bg-surface-elevated);
    padding: 10px 14px;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-subtle);
  }
</style>
