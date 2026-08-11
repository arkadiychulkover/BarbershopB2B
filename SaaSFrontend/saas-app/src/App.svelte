<script>
  import Router from 'svelte-spa-router';
  import Landing from './routes/Landing.svelte';
  import Register from './routes/Register.svelte';
  import Login from './routes/Login.svelte';
  import Payment from './routes/Payment.svelte';
  
  import Overview from './routes/Overview.svelte';
  import Settings from './routes/Settings.svelte';
  import Masters from './routes/Masters.svelte';
  import Services from './routes/Services.svelte';
  import Statistics from './routes/Statistics.svelte';
  import BotSetup from './routes/BotSetup.svelte';
  import Schedule from './routes/Schedule.svelte';

  const routes = {
    '/': Landing,
    '/register': Register,
    '/login': Login,
    '/payment': Payment,
    '/dashboard': Overview,
    '/dashboard/settings': Settings,
    '/dashboard/masters': Masters,
    '/dashboard/services': Services,
    '/dashboard/statistics': Statistics,
    '/dashboard/bot-setup': BotSetup,
    '/dashboard/schedule': Schedule,
  };
</script>

<Router {routes} />
