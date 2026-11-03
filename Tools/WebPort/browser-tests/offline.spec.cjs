const { test, expect } = require('@playwright/test');
test('the production worker caches generated build assets during the first online visit', async ({ page, context }) => {
  await page.goto('/offline/');
  await page.evaluate(async () => {
    await navigator.serviceWorker.register('service-worker.js');
    await navigator.serviceWorker.ready;
    if (!navigator.serviceWorker.controller) await new Promise(resolve => navigator.serviceWorker.addEventListener('controllerchange', resolve, {once:true}));
  });
  await context.setOffline(true);
  await page.reload();
  await expect(page).toHaveTitle('Offline fixture');
  for (const name of ['fixture.loader.js', 'fixture.data', 'fixture.wasm']) {
    const text = await page.evaluate(async name => (await fetch('Build/' + name)).text(), name);
    expect(text).toBe('First-session asset: Build/' + name);
  }
});
