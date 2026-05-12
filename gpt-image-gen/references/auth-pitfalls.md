# Auth Pitfalls — gpt-image-gen

## Codex OAuth token → 401 on api.openai.com

**症状：**
```
401 Unauthorized — Missing scopes: api.model.images.request
```

**原因：** `_read_codex_access_token()` 返回的是 ChatGPT/Codex OAuth token，验证域是 `chatgpt.com`。当脚本把它当 `OPENAI_API_KEY` 去打 `api.openai.com/v1/images/*`，Cloudflare WAF 返回 401。

**触发条件：**
- `OPENAI_API_KEY` 未设
- `~/.env` 和 `./.env` 都不存在或不含 OPENAI_API_KEY
- `hermes auth codex` 执行过 → Codex token 缓存在 Hermes auth store
- 脚本 auth 解析落到优先级 4（Codex token）

**解决：**
- **文生图：** 直接用 Hermes 内置 `image_generate` 工具（默认走 openai-codex provider → Codex backend），不走 gpt-image-gen 脚本
- **img2img / inpainting：** 必须有真正的 `OPENAI_API_KEY`，设为环境变量或写入 `~/.env`

**Codex backend 端点：** `https://chatgpt.com/backend-api/codex`（Responses API），不是 `api.openai.com`

---

## OPENAI_API_KEY env var defined but empty → "No API key found"\n\n**症状：**\n```\nNo API key found. Options:\n  • export OPENAI_API_KEY=***\n  • Create ~/.env with OPENAI_API_KEY=***\n  • Run `hermes auth codex`\n  • Pass --api-key\n```\n\n**但是** `echo $OPENAI_API_KEY` 显示变量已设置（`set: yes`）。\n\n**原因：** `resolve_api_key()` 用 `os.environ.get(\"OPENAI_API_KEY\", \"\").strip()` — 如果变量存在但值为空字符串，`.strip()` 返回 `\"\"`，falsy，被跳过。\n\n**诊断：** `python3 -c \"import os; k=os.environ.get('OPENAI_API_KEY',''); print(f'key_len={len(k)}')\"` → 输出 `key_len=0` 确认变量为空。\n\n**解决：** 设置非空值或写入 `~/.env`。\n\n---\n\n## Codex device_code "exhausted" — rate-limited, not expired\n\n**症状：**\n```\nhermes auth list\nopenai-codex (1 credentials):\n  #1  device_code  oauth  device_code exhausted (59m 2s left)\n```\n\n**原因：** Codex OAuth token 被 rate-limit，不是 token 过期。倒计时结束前无法刷新。\n\n**影响：** 内置 `image_generate` 工具（走 openai-codex provider）无法使用，直到倒计时归零。\n\n**解决：** 等待倒计时结束，或使用真正的 `OPENAI_API_KEY` 走标准 OpenAI REST API。\n\n---\n\n## moderation → images.edit 400

**症状（已修复）：**
```
Images.edit() got an unexpected keyword argument 'moderation'
```

**原因：** `generate()` 函数 shared payload 默认包含 `moderation`，但 `images.edit` 端点不接受该参数。

**修复：** `gen.py` 在 `has_images` 为 true 时自动 `payload.pop("moderation", None)`（commit 已打入脚本）。
