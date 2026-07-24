# Generate Chat Title API Contract

## Endpoint

`POST /generate-chat-title`

## Purpose

Generate a short chat title from the first user message. This endpoint only
generates a title; it must not run the normal chat/RAG answer flow.

## Request

Headers:

```http
Content-Type: application/json
```

Body:

```json
{
  "content": "Explain the difference between supervised and unsupervised learning."
}
```

Fields:

| Field | Type | Rules |
|---|---|---|
| `content` | string | Required, non-empty first user message. |

## Successful Response

Status: `200 OK`

Headers:

```http
Content-Type: application/json
```

The response body is a JSON string, not an object:

```json
"Supervised vs. Unsupervised Learning"
```

The returned title must be non-empty and should not include Markdown,
surrounding quotes, or the `[SYSTEM:GENERATING_TITLE]` marker.

## Error Responses

Use a non-2xx status code when a title cannot be generated. The backend treats
non-2xx responses, network failures, invalid JSON, and empty strings as failed
attempts.

## Backend Retry and Fallback

The backend makes one initial request and retries up to three additional times.
It waits one second between failed attempts. If all four attempts fail, it uses
the original `content` value as the chat title.
