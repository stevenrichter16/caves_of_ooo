const { defineConfig, devices } = require('@playwright/test');
module.exports = defineConfig({
  testDir: './browser-tests', timeout: 20000, workers: 2,
  use: { baseURL: 'http://127.0.0.1:8097', screenshot: 'only-on-failure', trace: 'retain-on-failure' },
  webServer: { command: 'python3 fixture_server.py', url: 'http://127.0.0.1:8097', reuseExistingServer: false },
  projects: [
    { name: 'chromium-phone', use: { ...devices['Pixel 7'], browserName: 'chromium' } },
    { name: 'webkit-iphone', use: { ...devices['iPhone 13'], browserName: 'webkit' } },
    { name: 'webkit-landscape', use: { ...devices['iPhone 13 landscape'], browserName: 'webkit' } }
  ]
});
