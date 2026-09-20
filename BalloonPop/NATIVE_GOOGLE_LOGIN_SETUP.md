# Native Google login setup

Balloon Pop uses Android Credential Manager. Login opens the native Google account sheet inside the game; it does not open an OAuth webpage or depend on an Android deep link.

## Google Cloud configuration

Use the same Google Cloud project as the web client ID in `Assets/Resources/AuthConfig.json`.

Create an OAuth client with application type **Android**:

- Package name: `com.gamixtv.BalloonPoP`
- Debug certificate SHA-1: `F6:65:04:6A:B3:F2:62:AD:6C:B9:22:6C:1E:74:6A:44:37:16:32:B6`

For a Play Store release, create another Android OAuth client using the SHA-1 from **Play Console → Setup → App integrity → App signing key certificate**. If a custom upload/release keystore is used outside Play App Signing, register that certificate's SHA-1 too.

Keep the Web OAuth client because its client ID is the token audience verified by the central auth server. The Android and Web clients must be in the same Google Cloud project.

## Server

The game exchanges the native Google ID token at:

```text
POST https://auth-server-2bis.onrender.com/auth/google/native
```

The server verifies the Google signature and audience, upserts one record in `balloonpop_users`, and returns an app-bound session token.
