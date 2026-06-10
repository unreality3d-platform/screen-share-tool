// api/livekit-token.js
// Issues a signed LiveKit access token. POST room + identity in, signed token out.
// The API key and secret are read from environment variables and never leave the server.

import { AccessToken } from 'livekit-server-sdk';

// Browser origins allowed to call this endpoint. An "origin" is the scheme + host
// with no path, e.g. https://unreality3d.com. Add the address(es) your published
// experience is served from. Keep the localhost entries while testing.
const ALLOWED_ORIGINS = new Set([
  'https://unreality3d.com',
  'https://www.unreality3d.com',
  'http://localhost:3000',
  'http://localhost:8080'
]);

const TOKEN_TTL = '6h';

function applyCorsHeaders(req, res) {
  const origin = req.headers.origin;
  if (origin && ALLOWED_ORIGINS.has(origin)) {
    res.setHeader('Access-Control-Allow-Origin', origin);
    res.setHeader('Vary', 'Origin');
    res.setHeader('Access-Control-Allow-Methods', 'POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    res.setHeader('Access-Control-Max-Age', '3600');
  }
}

function readBody(req) {
  if (!req.body) return {};
  if (typeof req.body === 'string') {
    try {
      return JSON.parse(req.body);
    } catch {
      return {};
    }
  }
  return req.body;
}

export default async function handler(req, res) {
  applyCorsHeaders(req, res);

  if (req.method === 'OPTIONS') {
    res.status(204).end();
    return;
  }

  if (req.method !== 'POST') {
    res.status(405).json({ error: 'Method not allowed' });
    return;
  }

  try {
    const apiKey = process.env.LIVEKIT_API_KEY;
    const apiSecret = process.env.LIVEKIT_API_SECRET;

    if (!apiKey || !apiSecret) {
      res.status(500).json({ error: 'Server is missing LiveKit credentials' });
      return;
    }

    const body = readBody(req);
    const room = typeof body.room === 'string' ? body.room.trim() : '';
    const identity = typeof body.identity === 'string' ? body.identity.trim() : '';
    const canPublish = body.canPublish === true;

    if (room.length === 0 || identity.length === 0) {
      res.status(400).json({ error: 'room and identity are required' });
      return;
    }

    // OPEN ENDPOINT (intentional for v1): anyone who can reach this can request a
    // token, including a screen-sharing token (canPublish). That's fine for a small,
    // trusted, private group. Before opening it to a wider or public audience, add a
    // caller check here (for example, verify a signed login your host issues) before
    // granting canPublish.
    const at = new AccessToken(apiKey, apiSecret, {
      identity,
      ttl: TOKEN_TTL
    });

    at.addGrant({
      roomJoin: true,
      room,
      canSubscribe: true,
      canPublish: canPublish,
      canPublishData: canPublish
    });

    const token = await at.toJwt();
    res.status(200).json({ token });
  } catch (error) {
    console.error('livekit-token error:', error && (error.code || error.message));
    res.status(500).json({ error: 'Token generation failed' });
  }
}
