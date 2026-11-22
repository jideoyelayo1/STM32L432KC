import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './tests/browser', outputDir: './artifacts/browser-tests', reporter: 'line',
  timeout: 45000, expect: { timeout: 15000 }, workers: 2,
  use: { baseURL: 'http://127.0.0.1:4177', headless: true, screenshot: 'only-on-failure', trace: 'retain-on-failure' },
  webServer: { command: 'python3 scripts/serve-browser.py', url: 'http://127.0.0.1:4177/demos/stm32', reuseExistingServer: false, timeout: 15000 },
  projects: ['chromium','webkit'].flatMap(browserName => [
    { name: `${browserName}-desktop`, use: { browserName, viewport: {width:1280,height:900} } },
    { name: `${browserName}-390`, use: { browserName, viewport: {width:390,height:844}, isMobile:true, hasTouch:true } },
    { name: `${browserName}-320`, use: { browserName, viewport: {width:320,height:740}, isMobile:true, hasTouch:true } }
  ])
});
