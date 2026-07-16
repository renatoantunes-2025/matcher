import { route } from './router.js';

window.addEventListener('hashchange', route);
window.addEventListener('DOMContentLoaded', route);
