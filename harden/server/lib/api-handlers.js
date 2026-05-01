import { readdir, readFile } from "fs/promises";
import { basename, join, dirname } from "path";
import { existsSync } from "fs";
import { fileURLToPath } from "url";
import { readPatterns, parseFrontmatter } from "../../scripts/lib/utils.js";
import { FILE_DOWNLOAD_PROVIDER_CONFIG_DIRS } from "../../lib/download-providers.js";
import {
	isAllowedBundleProvider,
	isAllowedFileProvider,
	isAllowedType,
	isValidId,
	sanitizeFilename
} from "./validation.js";

// Get project root directory (works in both Node.js and Bun, including Vercel)
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = join(__dirname, "..", "..");

// Helper to read file content (works in both Node.js and Bun)
async function readFileContent(filePath) {
	return readFile(filePath, "utf-8");
}

// Read all skills from source/skills/ subdirectories
export async function getSkills() {
	const skillsDir = join(PROJECT_ROOT, "source", "skills");
	const entries = await readdir(skillsDir, { withFileTypes: true });
	const skills = [];

	for (const entry of entries) {
		if (!entry.isDirectory()) continue;
		const skillMdPath = join(skillsDir, entry.name, "SKILL.md");
		if (!existsSync(skillMdPath)) continue;

		const content = await readFileContent(skillMdPath);
		const { frontmatter } = parseFrontmatter(content);

		skills.push({
			id: entry.name,
			name: frontmatter.name || entry.name,
			description: frontmatter.description || "No description available",
			userInvocable: frontmatter['user-invocable'] === true || frontmatter['user-invocable'] === 'true',
		});
	}

	return skills;
}

// Read a short tagline for a command from its editorial file
// (content/site/skills/<id>.md). Returns null if the file or tagline is
// missing. Taglines are used by UI surfaces that need a human-friendly
// one-liner; `description` stays optimized for auto-trigger matching.
async function readCommandTagline(id) {
	const editorialPath = join(PROJECT_ROOT, "content/site/skills", `${id}.md`);
	if (!existsSync(editorialPath)) return null;
	try {
		const raw = await readFileContent(editorialPath);
		const match = raw.match(/^---\n([\s\S]*?)\n---/);
		if (!match) return null;
		const taglineMatch = match[1].match(/tagline:\s*"([^"]+)"/);
		return taglineMatch ? taglineMatch[1] : null;
	} catch {
		return null;
	}
}

// Read commands. After the v3.0 consolidation, commands are sub-commands of
// /impeccable. Read them from command-metadata.json and include the root
// impeccable skill itself so UI surfaces (cheatsheet, magazine spread) can
// list them.
export async function getCommands() {
	const allSkills = await getSkills();
	const metadataPath = join(PROJECT_ROOT, "source/skills/impeccable/scripts/command-metadata.json");

	const commands = [];
	const impeccable = allSkills.find(s => s.name === "impeccable");
	if (impeccable) {
		commands.push({
			id: "impeccable",
			name: "impeccable",
			description: impeccable.description,
			tagline: await readCommandTagline("impeccable"),
			userInvocable: true,
		});
	}

	if (existsSync(metadataPath)) {
		try {
			const raw = await readFileContent(metadataPath);
			const metadata = JSON.parse(raw);
			for (const [id, meta] of Object.entries(metadata)) {
				commands.push({
					id,
					name: id,
					description: meta.description,
					tagline: await readCommandTagline(id),
					userInvocable: true,
				});
			}
		} catch (error) {
			console.error("Error reading command metadata:", error);
		}
	}

	// Fallback: return just user-invocable skills if no metadata
	if (commands.length === 0) {
		return allSkills.filter(s => s.userInvocable);
	}

	return commands;
}

// Get command/skill source content
export async function getCommandSource(id) {
	if (!isValidId(id)) {
		return { error: "Invalid command ID", status: 400 };
	}

	const skillPath = join(PROJECT_ROOT, "source", "skills", id, "SKILL.md");

	try {
		if (!existsSync(skillPath)) {
			return null;
		}
		const content = await readFileContent(skillPath);
		return content;
	} catch (error) {
		console.error("Error reading skill source:", error);
		return null;
	}
}

// Get the appropriate file path for a provider
export function getFilePath(type, provider, id) {
	const distDir = join(PROJECT_ROOT, "dist");
	const configDir = FILE_DOWNLOAD_PROVIDER_CONFIG_DIRS[provider];
	if (!configDir) return null;

	// Everything is a skill now
	if (type === "skill" || type === "command") {
		return join(distDir, provider, configDir, "skills", id, "SKILL.md");
	}

	return null;
}

// Handle individual file download
export async function handleFileDownload(type, provider, id) {
	if (!isAllowedType(type)) {
		return new Response("Invalid type", { status: 400 });
	}

	if (!isAllowedFileProvider(provider)) {
		return new Response("Invalid provider", { status: 400 });
	}

	if (!isValidId(id)) {
		return new Response("Invalid file ID", { status: 400 });
	}

	const filePath = getFilePath(type, provider, id);

	if (!filePath) {
		return new Response("Invalid provider", { status: 400 });
	}

	try {
		if (!existsSync(filePath)) {
			return new Response("File not found", { status: 404 });
		}

		const content = await readFile(filePath);
		const fileName = sanitizeFilename(basename(filePath));
		return new Response(content, {
			headers: {
				"Content-Type": "application/octet-stream",
				"Content-Disposition": `attachment; filename="${fileName}"`,
			},
		});
	} catch (error) {
		console.error("Error downloading file:", error);
		return new Response("Error downloading file", { status: 500 });
	}
}

// Extract patterns from SKILL.md using the shared utility
export async function getPatterns() {
	try {
		return readPatterns(PROJECT_ROOT);
	} catch (error) {
		console.error("Error reading patterns:", error);
		return { patterns: [], antipatterns: [] };
	}
}

// Handle bundle download
export async function handleBundleDownload(provider) {
	if (!isAllowedBundleProvider(provider)) {
		return new Response("Invalid provider", { status: 400 });
	}

	const distDir = join(PROJECT_ROOT, "dist");
	const zipPath = join(distDir, `${provider}.zip`);

	try {
		if (!existsSync(zipPath)) {
			return new Response("Bundle not found", { status: 404 });
		}

		const content = await readFile(zipPath);
		const safeProvider = sanitizeFilename(provider);
		return new Response(content, {
			headers: {
				"Content-Type": "application/zip",
				"Content-Disposition": `attachment; filename="impeccable-style-${safeProvider}.zip"`,
			},
		});
	} catch (error) {
		console.error("Error downloading bundle:", error);
		return new Response("Error downloading bundle", { status: 500 });
	}
}
