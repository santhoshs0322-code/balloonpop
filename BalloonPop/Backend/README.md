# Balloon Pop login API (Render + MongoDB Atlas)

1. Push this repository to GitHub and create a Render Blueprint using `Backend/render.yaml`. It builds the production image from `Backend/Dockerfile` using Node 22.
2. In Render, set `MONGODB_URI`, `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, and `PUBLIC_URL`. `PUBLIC_URL` is the service URL, for example `https://balloon-pop-api.onrender.com`.
3. In Google Cloud Console, create an OAuth 2.0 **Web application** client. Add this exact authorized redirect URI:
   `https://YOUR-RENDER-SERVICE.onrender.com/auth/google/callback`
4. Put the same Render URL in `Assets/Resources/AuthConfig.json`, without a trailing slash, then rebuild the Android app.
5. In Atlas Network Access, allow Render connectivity. Prefer Atlas private/static networking when available; otherwise use a tightly scoped database user and rotate its password.

Never commit the Atlas URI, Google client secret, or JWT secret. The unique `googleSub` index and atomic upsert guarantee one user document per Google account.

## Run with Docker locally

Copy `.env.example` to `.env`, add development credentials, then run:

```bash
docker compose up --build
```

The API is available at `http://localhost:3000`, and its health endpoint is `http://localhost:3000/health`. Google OAuth requires a browser-reachable HTTPS callback, so use the deployed Render URL for Android login testing.
