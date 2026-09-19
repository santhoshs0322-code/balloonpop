require("dotenv").config();
const crypto = require("crypto");
const express = require("express");
const cors = require("cors");
const jwt = require("jsonwebtoken");
const mongoose = require("mongoose");
const { OAuth2Client } = require("google-auth-library");

const required = ["MONGODB_URI", "GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET", "JWT_SECRET", "PUBLIC_URL"];
for (const key of required) {
  if (!process.env[key]) throw new Error(`Missing required environment variable: ${key}`);
}

const app = express();
app.set("trust proxy", 1);
app.use(cors({ origin: process.env.ALLOWED_ORIGIN || "*" }));
app.use(express.json());

const userSchema = new mongoose.Schema({
  googleSub: { type: String, required: true, unique: true, index: true },
  email: { type: String, required: true, lowercase: true },
  name: { type: String, required: true },
  picture: String,
  lastLoginAt: { type: Date, default: Date.now }
}, { timestamps: true, versionKey: false });
const User = mongoose.model("User", userSchema);

const stateSchema = new mongoose.Schema({
  value: { type: String, unique: true, index: true },
  expiresAt: { type: Date, expires: 0 }
}, { versionKey: false });
const OAuthState = mongoose.model("OAuthState", stateSchema);

const revokedSchema = new mongoose.Schema({
  jti: { type: String, unique: true, index: true },
  expiresAt: { type: Date, expires: 0 }
}, { versionKey: false });
const RevokedToken = mongoose.model("RevokedToken", revokedSchema);

const redirectUri = `${process.env.PUBLIC_URL.replace(/\/$/, "")}/auth/google/callback`;
const google = new OAuth2Client(process.env.GOOGLE_CLIENT_ID, process.env.GOOGLE_CLIENT_SECRET, redirectUri);

function issueToken(user) {
  return jwt.sign({ sub: user.id }, process.env.JWT_SECRET, {
    expiresIn: "30d", jwtid: crypto.randomUUID(), issuer: "balloon-pop-api", audience: "balloon-pop-game"
  });
}

async function requireAuth(req, res, next) {
  try {
    const token = (req.headers.authorization || "").replace(/^Bearer\s+/i, "");
    if (!token) return res.status(401).json({ ok: false, message: "Missing session" });
    const payload = jwt.verify(token, process.env.JWT_SECRET, { issuer: "balloon-pop-api", audience: "balloon-pop-game" });
    if (await RevokedToken.exists({ jti: payload.jti })) return res.status(401).json({ ok: false, message: "Session revoked" });
    req.auth = payload;
    req.token = token;
    next();
  } catch (_) {
    res.status(401).json({ ok: false, message: "Invalid or expired session" });
  }
}

app.get("/health", (_req, res) => res.json({ ok: true }));

app.get("/auth/google", async (_req, res, next) => {
  try {
    const state = crypto.randomBytes(32).toString("hex");
    await OAuthState.create({ value: state, expiresAt: new Date(Date.now() + 10 * 60 * 1000) });
    res.redirect(google.generateAuthUrl({
      access_type: "online", scope: ["openid", "email", "profile"], state, prompt: "select_account"
    }));
  } catch (error) { next(error); }
});

app.get("/auth/google/callback", async (req, res) => {
  try {
    const state = await OAuthState.findOneAndDelete({ value: req.query.state, expiresAt: { $gt: new Date() } });
    if (!state || !req.query.code) return res.redirect("balloonpop://auth?error=invalid_request");
    const { tokens } = await google.getToken(req.query.code);
    const ticket = await google.verifyIdToken({ idToken: tokens.id_token, audience: process.env.GOOGLE_CLIENT_ID });
    const profile = ticket.getPayload();
    if (!profile || !profile.sub || !profile.email_verified) return res.redirect("balloonpop://auth?error=unverified_email");

    // googleSub is unique: every Google account has exactly one document.
    const user = await User.findOneAndUpdate(
      { googleSub: profile.sub },
      { $set: { email: profile.email, name: profile.name || profile.email, picture: profile.picture || "", lastLoginAt: new Date() } },
      { new: true, upsert: true, setDefaultsOnInsert: true }
    );
    res.redirect(`balloonpop://auth?token=${encodeURIComponent(issueToken(user))}`);
  } catch (error) {
    console.error("OAuth callback failed", error.message);
    res.redirect("balloonpop://auth?error=login_failed");
  }
});

app.get("/api/me", requireAuth, async (req, res) => {
  const user = await User.findById(req.auth.sub).lean();
  if (!user) return res.status(404).json({ ok: false, message: "User not found" });
  res.json({ ok: true, user: {
    id: user._id.toString(), name: user.name, email: user.email, picture: user.picture,
    createdAt: user.createdAt.toISOString(), lastLoginAt: user.lastLoginAt.toISOString()
  }});
});

app.post("/auth/logout", requireAuth, async (req, res) => {
  await RevokedToken.updateOne(
    { jti: req.auth.jti },
    { $setOnInsert: { jti: req.auth.jti, expiresAt: new Date(req.auth.exp * 1000) } },
    { upsert: true }
  );
  res.json({ ok: true });
});

app.use((error, _req, res, _next) => {
  console.error(error);
  res.status(500).json({ ok: false, message: "Server error" });
});

const port = process.env.PORT || 3000;
mongoose.connect(process.env.MONGODB_URI)
  .then(() => app.listen(port, () => console.log(`Balloon Pop API listening on ${port}`)))
  .catch(error => { console.error("MongoDB connection failed", error); process.exit(1); });
