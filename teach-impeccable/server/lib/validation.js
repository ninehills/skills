// Shared validation helpers for input sanitization

import {
  BUNDLE_DOWNLOAD_PROVIDERS,
  DOWNLOAD_PROVIDERS,
  FILE_DOWNLOAD_PROVIDERS,
} from '../../lib/download-providers.js';

// Only allow alphanumeric, hyphens, and underscores in IDs
export const VALID_ID = /^[a-zA-Z0-9_-]+$/;

export const ALLOWED_PROVIDERS = DOWNLOAD_PROVIDERS;
export const ALLOWED_FILE_PROVIDERS = FILE_DOWNLOAD_PROVIDERS;
export const ALLOWED_BUNDLE_PROVIDERS = BUNDLE_DOWNLOAD_PROVIDERS;
export const ALLOWED_TYPES = ['skill', 'command'];

export function isValidId(id) {
  return typeof id === 'string' && VALID_ID.test(id);
}

export function isAllowedProvider(provider) {
  return ALLOWED_PROVIDERS.includes(provider);
}

export function isAllowedFileProvider(provider) {
  return ALLOWED_FILE_PROVIDERS.includes(provider);
}

export function isAllowedBundleProvider(provider) {
  return ALLOWED_BUNDLE_PROVIDERS.includes(provider);
}

export function isAllowedType(type) {
  return ALLOWED_TYPES.includes(type);
}

// Sanitize a filename for use in Content-Disposition headers
export function sanitizeFilename(filename) {
  return filename.replace(/[^a-zA-Z0-9._-]/g, '');
}
