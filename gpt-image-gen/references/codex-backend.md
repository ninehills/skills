# Codex Backend Integration

The script auto-detects auth type and routes accordingly:

- **JWT token** (starts with `eyJ`) → Codex Responses API
- **sk-... key** → Standard OpenAI REST API

## Codex Endpoint

```
POST https://chatgpt.com/backend-api/codex/responses
```

Host model: `gpt-5.4` — only used to invoke the `image_generation` tool; the actual image work is done by `gpt-image-2`.

## Required Headers

```python
{
    "User-Agent": "codex_cli_rs/0.0.0 (Hermes Agent)",
    "originator": "codex_cli_rs",
    "ChatGPT-Account-ID": "<extracted from JWT chatgpt_account_id claim>",
}
```

Without `originator: codex_cli_rs`, Cloudflare returns 403 regardless of auth correctness.

## txt2img Flow

```
client.responses.stream(
    model="gpt-5.4",
    input=[{"type": "message", "role": "user", "content": [{"type": "input_text", "text": prompt}]}],
    tools=[{"type": "image_generation", "model": "gpt-image-2", "size": ..., "quality": ..., ...}],
    tool_choice={"type": "allowed_tools", "mode": "required", "tools": [{"type": "image_generation"}]},
)
```

Extract b64 from `response.output_item.done` events where `item.type == "image_generation_call"`.

## img2img (Reference Images)

Codex `image_generation` tool has no reference-image parameter. Workaround: include reference images as `input_image` content items alongside the text prompt:

```python
content = [
    {"type": "input_text", "text": prompt},
    {"type": "input_image", "image_url": "data:image/jpeg;base64,..."},
]
```

The chat model sees the reference and uses it as context for the image generation tool call.

## Limitations

- **No mask inpainting**: Codex backend refuses `--mask` (exit code 2)
- **n=1 only**: multi-image not supported via Responses API
- **PNG output only**: `output_format` is hardcoded to `png`
- **No `moderation` or `input_fidelity`**: these are REST API params, not applicable to Responses API

## Auth Resolution Order

1. `OPENAI_API_KEY` env var (standard API key → REST path)
2. `./.env` / `~/.env`
3. Codex OAuth token via `agent.auxiliary_client._read_codex_access_token` → Codex path
4. `config.yaml` → `image_gen.openai.api_key`

Token is classified as Codex vs standard by checking `key.startswith("eyJ")`.
