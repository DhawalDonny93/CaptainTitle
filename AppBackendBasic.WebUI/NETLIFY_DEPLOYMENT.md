# Netlify Deployment Guide

## Issue: Page Not Found on Deployed Site

If you're seeing "Page not found" errors on routes like `/auth/google/callback` on your deployed Netlify site, it's because Netlify needs special configuration for Single Page Applications (SPAs) that use client-side routing.

## Solution

Two files have been created to fix this issue:

### 1. `public/_redirects`

This file tells Netlify to redirect all routes to `index.html` so React Router can handle routing:

```
/* /index.html 200
```

**How it works:**
- `/*` - Matches all routes
- `/index.html` - Redirects to the main HTML file
- `200` - Returns HTTP 200 (success) instead of 404

### 2. `netlify.toml`

This is Netlify's configuration file:

```toml
[build]
  command = "npm run build"
  publish = "dist"

[[redirects]]
  from = "/*"
  to = "/index.html"
  status = 200
```

**Configuration explained:**
- `command` - Build command to run
- `publish` - Directory containing built files (Vite uses `dist`)
- `redirects` - Same redirect rule as `_redirects` file

## Deployment Steps

### Option 1: Automatic Deployment (Recommended)

If your Netlify site is connected to your Git repository:

1. **Commit the new files:**
   ```bash
   git add public/_redirects netlify.toml
   git commit -m "Add Netlify SPA routing configuration"
   git push
   ```

2. **Netlify will automatically:**
   - Detect the push
   - Run `npm run build`
   - Deploy the new version with routing configuration
   - Your callback routes will now work!

### Option 2: Manual Deployment

If deploying manually via Netlify CLI:

1. **Install Netlify CLI (if not already installed):**
   ```bash
   npm install -g netlify-cli
   ```

2. **Login to Netlify:**
   ```bash
   netlify login
   ```

3. **Build your project:**
   ```bash
   npm run build
   ```

4. **Deploy:**
   ```bash
   netlify deploy --prod
   ```

### Option 3: Drag & Drop Deployment

1. **Build locally:**
   ```bash
   npm run build
   ```

2. **Ensure `_redirects` is in the `dist` folder:**
   - Vite automatically copies files from `public/` to `dist/` during build
   - Verify `dist/_redirects` exists after build

3. **Drag the `dist` folder to Netlify:**
   - Go to https://app.netlify.com/
   - Navigate to your site
   - Go to "Deploys" tab
   - Drag the `dist` folder to the upload area

## Verification

After deployment, test these URLs:

1. **Homepage:**
   ```
   https://basicframeworkwebui.netlify.app/
   ```
   ✅ Should load normally

2. **Login page:**
   ```
   https://basicframeworkwebui.netlify.app/login
   ```
   ✅ Should load the login page

3. **Google callback:**
   ```
   https://basicframeworkwebui.netlify.app/auth/google/callback
   ```
   ✅ Should load the callback page (not 404)

4. **With hash parameters:**
   ```
   https://basicframeworkwebui.netlify.app/auth/google/callback#id_token=test
   ```
   ✅ Should load and show "Processing Authentication"

## Troubleshooting

### Issue: Still getting 404 after deployment

**Check 1: Verify `_redirects` file is in the build output**
```bash
npm run build
ls -la dist/_redirects
```
If the file doesn't exist, Vite didn't copy it. Ensure it's in the `public/` folder.

**Check 2: Check Netlify build logs**
1. Go to https://app.netlify.com/
2. Navigate to your site
3. Click "Deploys"
4. Click on the latest deploy
5. Check the build logs for errors

**Check 3: Verify Netlify configuration**
1. Go to Site Settings → Build & deploy → Build settings
2. Verify:
   - Build command: `npm run build`
   - Publish directory: `dist`

### Issue: Redirects not working

**Solution 1: Clear Netlify cache**
```bash
netlify deploy --prod --clear-cache
```

**Solution 2: Check redirect rules in Netlify dashboard**
1. Go to Site Settings → Build & deploy → Post processing
2. Check "Redirect rules"
3. Should show: `/* → /index.html (200)`

### Issue: Build fails on Netlify

**Common causes:**
1. **Node version mismatch**
   - Add `.nvmrc` file with your Node version:
     ```
     18.17.0
     ```
   - Or set in `netlify.toml`:
     ```toml
     [build.environment]
       NODE_VERSION = "18.17.0"
     ```

2. **Missing dependencies**
   - Ensure `package-lock.json` or `yarn.lock` is committed
   - Check all dependencies are in `package.json`

3. **Environment variables**
   - Set in Netlify: Site Settings → Environment variables
   - Add: `VITE_API_URL`, `VITE_GOOGLE_CLIENT_ID`, etc.

## Local Testing

To test locally that routing works correctly:

```bash
# Build the project
npm run build

# Serve the built files
npx serve dist

# Test the callback route
curl http://localhost:3000/auth/google/callback
```

Should return the HTML content, not a 404.

## Google Cloud Console Configuration

After deploying, update your Google OAuth redirect URIs:

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project
3. Navigate to: APIs & Services → Credentials
4. Click on your OAuth 2.0 Client ID
5. Add to "Authorized redirect URIs":
   ```
   https://basicframeworkwebui.netlify.app/auth/google/callback
   ```
6. Click "Save"

## Production Checklist

Before going live, ensure:

- [ ] `_redirects` file exists in `public/` folder
- [ ] `netlify.toml` is in project root
- [ ] Environment variables set in Netlify dashboard
- [ ] Google OAuth redirect URI updated
- [ ] Build succeeds locally: `npm run build`
- [ ] All routes tested on deployed site
- [ ] HTTPS enabled (Netlify does this automatically)
- [ ] Custom domain configured (if applicable)
- [ ] Error tracking set up (e.g., Sentry)

## Additional Netlify Features

### Custom Headers

Add to `netlify.toml` for security:

```toml
[[headers]]
  for = "/*"
  [headers.values]
    X-Frame-Options = "DENY"
    X-Content-Type-Options = "nosniff"
    X-XSS-Protection = "1; mode=block"
    Referrer-Policy = "strict-origin-when-cross-origin"
```

### Environment-Specific Builds

```toml
[context.production.environment]
  VITE_API_URL = "https://api.yourapp.com/api"

[context.deploy-preview.environment]
  VITE_API_URL = "https://staging-api.yourapp.com/api"

[context.branch-deploy.environment]
  VITE_API_URL = "https://dev-api.yourapp.com/api"
```

### Build Plugins

Add useful plugins in `netlify.toml`:

```toml
[[plugins]]
  package = "@netlify/plugin-lighthouse"

[[plugins]]
  package = "netlify-plugin-cache"
```

## Performance Optimization

### Enable Asset Optimization

In Netlify dashboard:
1. Site Settings → Build & deploy → Post processing
2. Enable:
   - Bundle CSS
   - Minify CSS
   - Minify JS
   - Compress images

### Add Cache Headers

```toml
[[headers]]
  for = "/assets/*"
  [headers.values]
    Cache-Control = "public, max-age=31536000, immutable"
```

## Monitoring

### Deploy Notifications

Set up in Netlify:
1. Site Settings → Build & deploy → Deploy notifications
2. Add notifications for:
   - Deploy started
   - Deploy succeeded
   - Deploy failed

### Analytics

Enable Netlify Analytics:
1. Site Settings → Analytics
2. Enable "Netlify Analytics"
3. View traffic, popular pages, and sources

## Support

If you continue to have issues:

1. **Netlify Support Docs:** https://docs.netlify.com/
2. **Netlify Community:** https://answers.netlify.com/
3. **Check build logs:** Netlify dashboard → Deploys → [Latest deploy] → Deploy log
4. **Test locally:** Ensure the issue isn't in your code

## Summary

The "Page not found" error on `/auth/google/callback` was caused by Netlify not knowing how to handle client-side routes. The `_redirects` file and `netlify.toml` configuration tell Netlify to serve `index.html` for all routes, allowing React Router to handle the routing client-side.

After committing and pushing these files, your deployed site will properly handle all routes including the Google OAuth callback!
