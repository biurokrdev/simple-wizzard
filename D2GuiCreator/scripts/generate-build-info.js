const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

function getBuildInfo() {
  let gitHash = 'unknown';
  let gitBranch = 'unknown';
  let buildNumber = '00000';

  try {
    gitHash = execSync('git rev-parse --short HEAD').toString().trim();
  } catch (e) {
    console.warn('Could not get git hash:', e.message);
  }

  try {
    gitBranch = execSync('git rev-parse --abbrev-ref HEAD').toString().trim();
    // Jeśli jesteśmy w detached HEAD, spróbuj pobrać z remote
    if (gitBranch === 'HEAD') {
      try {
        gitBranch = execSync('git branch -r --contains HEAD').toString().trim().split('\n')[0].trim();
      } catch (e) {
        gitBranch = 'detached HEAD';
      }
    }
  } catch (e) {
    console.warn('Could not get git branch:', e.message);
  }

  try {
    // Numer buildu to liczba commitów
    buildNumber = execSync('git rev-list --count HEAD').toString().trim().padStart(5, '0');
  } catch (e) {
    console.warn('Could not get build number:', e.message);
  }

  const now = new Date();
  const buildDate = `${now.getDate().toString().padStart(2, '0')}.${(now.getMonth() + 1).toString().padStart(2, '0')}.${now.getFullYear()}, ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:${now.getSeconds().toString().padStart(2, '0')}`;

  return {
    buildNumber,
    gitHash,
    gitBranch,
    buildDate,
    timestamp: now.toISOString()
  };
}

const buildInfo = getBuildInfo();

console.log('Build Info:', buildInfo);

// Zapisz do pliku TypeScript
const content = `// Auto-generated file - do not edit manually
// Generated at: ${buildInfo.buildDate}

export const BUILD_INFO = ${JSON.stringify(buildInfo, null, 2)};

// Dodaj do globalnego window dla runtime access
if (typeof window !== 'undefined') {
  (window as any).__BUILD_INFO__ = BUILD_INFO;
}
`;

const outputPath = path.join(__dirname, '../src/build-info.ts');
fs.writeFileSync(outputPath, content, 'utf8');

console.log(`Build info generated at: ${outputPath}`);
