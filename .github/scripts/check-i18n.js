import { readFileSync } from 'fs';

function collectKeys(obj, prefix = '') {
  const keys = [];
  for (const [k, v] of Object.entries(obj)) {
    const full = prefix ? `${prefix}.${k}` : k;
    if (v !== null && typeof v === 'object' && !Array.isArray(v)) {
      keys.push(...collectKeys(v, full));
    } else {
      keys.push(full);
    }
  }
  return keys;
}

const en = JSON.parse(readFileSync('frontend/src/locales/en.json', 'utf8'));
const ru = JSON.parse(readFileSync('frontend/src/locales/ru.json', 'utf8'));

const enKeys = new Set(collectKeys(en));
const ruKeys = new Set(collectKeys(ru));

const onlyEn = [...enKeys].filter(k => !ruKeys.has(k));
const onlyRu = [...ruKeys].filter(k => !enKeys.has(k));

if (onlyEn.length || onlyRu.length) {
  if (onlyEn.length) console.error('Missing in ru.json:\n' + onlyEn.map(k => `  ${k}`).join('\n'));
  if (onlyRu.length) console.error('Missing in en.json:\n' + onlyRu.map(k => `  ${k}`).join('\n'));
  process.exit(1);
}

console.log(`✓ ${enKeys.size} keys in sync`);
