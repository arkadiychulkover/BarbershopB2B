<script>
  import Router from 'svelte-spa-router';
  import { currentLocale } from './lib/locale.js';
  import Landing from './routes/Landing.svelte';
  import Register from './routes/Register.svelte';
  import Login from './routes/Login.svelte';
  import ForgotPassword from './routes/ForgotPassword.svelte';
  import ResetPassword from './routes/ResetPassword.svelte';
  import Payment from './routes/Payment.svelte';
  
  import Overview from './routes/Overview.svelte';
  import Settings from './routes/Settings.svelte';
  import Masters from './routes/Masters.svelte';
  import Clients from './routes/Clients.svelte';
  import Services from './routes/Services.svelte';
  import Statistics from './routes/Statistics.svelte';
  import BotSetup from './routes/BotSetup.svelte';
  import Schedule from './routes/Schedule.svelte';
  import Timeline from './routes/Timeline.svelte';
  import Admin from './routes/Admin.svelte';

  // Application route definitions for ARCH SYSTEM
  const routes = {
    '/': Landing,
    '/register': Register,
    '/login': Login,
    '/forgot-password': ForgotPassword,
    '/reset-password': ResetPassword,
    '/payment': Payment,
    '/admin': Admin,
    '/dashboard': Overview,
    '/dashboard/settings': Settings,
    '/dashboard/masters': Masters,
    '/dashboard/clients': Clients,
    '/dashboard/services': Services,
    '/dashboard/statistics': Statistics,
    '/dashboard/bot-setup': BotSetup,
    '/dashboard/schedule': Schedule,
    '/dashboard/timeline': Timeline,
  };
</script>

{#key $currentLocale}
  <Router {routes} />
{/key}
