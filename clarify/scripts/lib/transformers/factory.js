import path from 'path';
import { cleanDir, ensureDir, writeFile, generateYamlFrontmatter, generateYamlDocument, replacePlaceholders } from '../utils.js';
import { SKILL_CATEGORIES, CATEGORY_ORDER } from '../sub-pages-data.js';

/**
 * Map from frontmatter field name to extraction spec.
 *
 * - sourceKey: property name on the skill object
 * - yamlKey: key name in YAML frontmatter
 * - condition: if provided, field is only emitted when this returns true
 * - value: if provided, use this instead of skill[sourceKey]
 */
const FIELD_SPECS = {
  'user-invocable': {
    sourceKey: 'userInvocable',
    yamlKey: 'user-invocable',
    condition: (skill) => skill.userInvocable,
    value: () => true,
  },
  'argument-hint': {
    sourceKey: 'argumentHint',
    yamlKey: 'argument-hint',
    condition: (skill) => skill.userInvocable && skill.argumentHint,
  },
  license: {
    sourceKey: 'license',
    yamlKey: 'license',
  },
  compatibility: {
    sourceKey: 'compatibility',
    yamlKey: 'compatibility',
  },
  metadata: {
    sourceKey: 'metadata',
    yamlKey: 'metadata',
  },
  'allowed-tools': {
    sourceKey: 'allowedTools',
    yamlKey: 'allowed-tools',
  },
};

function humanizeSkillName(name) {
  return name
    .split('-')
    .map(part => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');
}

function summarizeDescription(description, maxLength = 88) {
  if (!description || description.length <= maxLength) return description;
  const clipped = description.slice(0, maxLength - 1);
  const lastSpace = clipped.lastIndexOf(' ');
  return `${(lastSpace > 48 ? clipped.slice(0, lastSpace) : clipped).trimEnd()}...`;
}

function buildOpenAIMetadata(skill) {
  const displayName = humanizeSkillName(skill.name);
  return {
    interface: {
      display_name: displayName,
      short_description: summarizeDescription(skill.description),
      default_prompt: `Use ${displayName} to redesign, critique, audit, or polish this frontend.`,
    },
  };
}

/**
 * Create a transformer function for a given provider config.
 *
 * @param {Object} config - Provider configuration from providers.js
 * @returns {Function} transform(skills, distDir, options?)
 */
export function createTransformer(config) {
  const { provider, configDir, displayName, frontmatterFields = [], bodyTransform, placeholderProvider, writeOpenAIMetadata = false, includeVersion = true } = config;
  const placeholderKey = placeholderProvider || provider;

  const activeFields = frontmatterFields
    .map((name) => FIELD_SPECS[name])
    .filter(Boolean);

  return function transform(skills, distDir, options = {}) {
    const { skillsVersion = '' } = options;
    const providerDir = path.join(distDir, provider);
    const skillsDir = path.join(providerDir, `${configDir}/skills`);

    cleanDir(providerDir);
    ensureDir(skillsDir);

    const allSkillNames = skills.map((s) => s.name);
    const commandNames = skills
      .filter((s) => s.userInvocable)
      .map((s) => s.name);

    let refCount = 0;
    let scriptCount = 0;

    for (const skill of skills) {
      const skillName = skill.name;
      const skillDir = path.join(skillsDir, skillName);

      // Build frontmatter
      const frontmatterObj = {
        name: skillName,
        description: skill.description,
      };
      if (skillsVersion && includeVersion) frontmatterObj.version = skillsVersion;

      for (const spec of activeFields) {
        if (spec.condition && !spec.condition(skill)) continue;
        const val = spec.value ? spec.value(skill) : skill[spec.sourceKey];
        if (val) frontmatterObj[spec.yamlKey] = val;
      }

      // Replace {{command_hint}} in argument-hint with command names from metadata,
      // grouped by category with middle dots between groups for natural line-breaking.
      if (frontmatterObj['argument-hint']?.includes('{{command_hint}}')) {
        const metaScript = skill.scripts?.find(s => s.name === 'command-metadata.json');
        if (metaScript) {
          const commands = Object.keys(JSON.parse(metaScript.content));
          // Derive groups from SKILL_CATEGORIES, excluding the parent skill name
          const grouped = CATEGORY_ORDER
            .map(cat => commands.filter(c => SKILL_CATEGORIES[c] === cat).join('|'))
            .filter(Boolean)
            .join(' · ');
          frontmatterObj['argument-hint'] = frontmatterObj['argument-hint'].replace(
            '{{command_hint}}',
            grouped
          );
        }
      }

      const frontmatter = generateYamlFrontmatter(frontmatterObj);

      // Build body
      let skillBody = replacePlaceholders(skill.body, placeholderKey, commandNames, allSkillNames);

      // Replace {{scripts_path}} with provider-aware path to skill's scripts directory
      const scriptsPath = `${configDir}/skills/${skillName}/scripts`;
      skillBody = skillBody.replace(/\{\{scripts_path\}\}/g, scriptsPath);
      if (bodyTransform) skillBody = bodyTransform(skillBody, skill);

      const content = `${frontmatter}\n\n${skillBody}`;
      writeFile(path.join(skillDir, 'SKILL.md'), content);

      if (writeOpenAIMetadata) {
        const openaiMetadata = buildOpenAIMetadata(skill);
        writeFile(path.join(skillDir, 'agents', 'openai.yaml'), generateYamlDocument(openaiMetadata));
      }

      // Copy reference files
      if (skill.references && skill.references.length > 0) {
        const refDir = path.join(skillDir, 'reference');
        ensureDir(refDir);
        for (const ref of skill.references) {
          let refContent = replacePlaceholders(ref.content, placeholderKey, [], allSkillNames);
          refContent = refContent.replace(/\{\{scripts_path\}\}/g, scriptsPath);
          writeFile(path.join(refDir, `${ref.name}.md`), refContent);
          refCount++;
        }
      }

      // Copy script files
      if (skill.scripts && skill.scripts.length > 0) {
        const scriptsOutDir = path.join(skillDir, 'scripts');
        ensureDir(scriptsOutDir);
        for (const script of skill.scripts) {
          writeFile(path.join(scriptsOutDir, script.name), script.content);
          scriptCount++;
        }
      }
    }

    const skillWord = skills.length === 1 ? 'skill' : 'skills';
    const refInfo = refCount > 0 ? ` (${refCount} reference files)` : '';
    const scriptInfo = scriptCount > 0 ? ` (${scriptCount} script files)` : '';
    console.log(`✓ ${displayName}: ${skills.length} ${skillWord}${refInfo}${scriptInfo}`);
  };
}
