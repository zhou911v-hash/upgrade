import { chromium } from 'playwright';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const previewPath = `file://${path.join(__dirname, 'preview.html')}`;

const shots = [
  { view: 'login',     size: { w: 1200, h: 900 }, name: '00-login.png' },
  { view: 'dashboard', size: { w: 1440, h: 820 }, name: '01-dashboard.png' },
  { view: 'floor',     size: { w: 1440, h: 820 }, name: '02-floor-monitor.png' },
  { view: 'three',     size: { w: 1440, h: 820 }, name: '03-3d-monitor.png' },
  { view: 'group',     size: { w: 1440, h: 820 }, name: '04-group-control.png' },
  { view: 'scene',     size: { w: 1440, h: 820 }, name: '05-scene-mode.png' },
  { view: 'schedule',  size: { w: 1440, h: 820 }, name: '06-schedule.png' },
  { view: 'energy',    size: { w: 1440, h: 820 }, name: '07-energy-stats.png' },
  { view: 'log',       size: { w: 1440, h: 820 }, name: '08-log.png' },
  { view: 'settings',  size: { w: 1440, h: 820 }, name: '09-settings.png' },
];

const browser = await chromium.launch();
for (const { view, size, name } of shots) {
  const context = await browser.newContext({
    viewport: { width: size.w, height: size.h },
    deviceScaleFactor: 2,
  });
  const page = await context.newPage();
  await page.goto(`${previewPath}?view=${view}`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(400);
  const out = path.join(__dirname, 'screenshots', name);
  await page.screenshot({ path: out, fullPage: false });
  console.log(`  ✓ ${name}`);
  await context.close();
}
await browser.close();
console.log('Done.');
