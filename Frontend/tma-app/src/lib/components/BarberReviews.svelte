<script lang="ts">
  import { onMount } from 'svelte';
  import { apiFetch } from '../api';
  
  let reviews = [];
  let loading = true;
  let error = null;

  onMount(async () => {
    loading = true;
    try {
      reviews = await apiFetch('/api/Barber/my-reviews');
    } catch (e) {
      console.error(e);
      error = "Не удалось загрузить отзывы";
    } finally {
      loading = false;
    }
  });
  
  function renderStars(rating) {
    let stars = '';
    for(let i=1; i<=5; i++) {
        if (i <= rating) stars += '★';
        else stars += '☆';
    }
    return stars;
  }
</script>

<div class="reviews-container">
  {#if loading}
    <div class="status-msg">Загрузка отзывов...</div>
  {:else if error}
    <div class="status-msg error">{error}</div>
  {:else if reviews.length === 0}
    <div class="empty-state">
      <div class="icon">⭐</div>
      <p>У вас пока нет отзывов.</p>
    </div>
  {:else}
    <div class="reviews-list">
      {#each reviews as review}
        <div class="review-card">
          <div class="review-header">
            <span class="client-name">{review.clientName}</span>
            <span class="review-date">{new Date(review.createdAt).toLocaleDateString('ru-RU')}</span>
          </div>
          <div class="review-rating">{renderStars(review.rating)}</div>
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
    padding-bottom: 24px;
  }
  
  .status-msg, .empty-state {
    text-align: center;
    padding: 40px 20px;
    color: var(--tg-theme-hint-color, #999);
  }
  
  .error {
    color: #F44336;
  }
  
  .empty-state .icon {
    font-size: 48px;
    margin-bottom: 16px;
  }
  
  .reviews-list {
    display: flex;
    flex-direction: column;
    gap: 16px;
  }
  
  .review-card {
    background: var(--tg-theme-bg-color, #fff);
    border-radius: 12px;
    padding: 16px;
    box-shadow: 0 1px 4px rgba(0,0,0,0.05);
  }
  
  .review-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 8px;
  }
  
  .client-name {
    font-weight: 600;
    font-size: 16px;
    color: var(--tg-theme-text-color, #000);
  }
  
  .review-date {
    font-size: 12px;
    color: var(--tg-theme-hint-color, #999);
  }
  
  .review-rating {
    color: #FFC107;
    font-size: 18px;
    margin-bottom: 8px;
    letter-spacing: 2px;
  }
  
  .review-comment {
    font-size: 14px;
    color: var(--tg-theme-text-color, #000);
    line-height: 1.4;
  }
</style>
