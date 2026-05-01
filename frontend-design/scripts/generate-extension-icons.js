#!/usr/bin/env node

/**
 * Generates PNG extension icons from SVG using Puppeteer.
 *
 * Run: node scripts/generate-extension-icons.js
 */

import puppeteer from 'puppeteer';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, '..');
const ICONS_DIR = path.join(ROOT, 'extension/icons');

const SIZES = [16, 32, 48, 128];

const svgContent = fs.readFileSync(path.join(ICONS_DIR, 'icon.svg'), 'utf-8');

const browser = await puppeteer.launch({ headless: true });
const page = await browser.newPage();

for (const size of SIZES) {
  await page.setViewport({ width: size, height: size, deviceScaleFactor: 1 });
  await page.setContent(`
    <!DOCTYPE html>
    <html>
    <head><style>* { margin: 0; padding: 0; } body { width: ${size}px; height: ${size}px; overflow: hidden; }</style></head>
    <body>${svgContent.replace('viewBox="0 0 128 128"', `viewBox="0 0 128 128" width="${size}" height="${size}"`)}</body>
    </html>
  `);
  await page.screenshot({ path: path.join(ICONS_DIR, `icon-${size}.png`), omitBackground: true });
  console.log(`Generated icon-${size}.png`);
}

await browser.close();
