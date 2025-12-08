/**
 * Browser Console Debug Script
 *
 * Run this script in the browser console at:
 * https://thankful-mushroom-09f42810f.3.azurestaticapps.net/
 *
 * This will help diagnose authentication issues
 */

console.log("=== TimePunchClock Authentication Debug ===\n");

// 1. Check if configuration is loaded
console.log("1. Checking Configuration...");
fetch('/appsettings.Production.json')
  .then(r => r.json())
  .then(config => {
    console.log("✅ Configuration loaded:");
    console.log("   - Authentication Enabled:", config.Authentication?.Enabled);
    console.log("   - Auth Provider:", config.AuthProvider);
    console.log("   - Backend URL:", config.TimeClientBaseUrl);
    console.log("   - Azure AD Authority:", config.AzureAd?.Authority);
    console.log("   - Blazor Client ID:", config.AzureAd?.ClientId);
    console.log("   - API Scopes:", config.Api?.Scopes);

    // Check for placeholders
    const hasPlaceholders =
      config.TimeClientBaseUrl?.includes("PLACEHOLDER") ||
      config.AzureAd?.Authority?.includes("PLACEHOLDER") ||
      config.AzureAd?.ClientId?.includes("PLACEHOLDER") ||
      config.Api?.Scopes?.some(s => s.includes("PLACEHOLDER"));

    if (hasPlaceholders) {
      console.error("❌ Configuration contains PLACEHOLDER values! Deployment may have failed.");
    } else {
      console.log("✅ No placeholder values found in configuration");
    }
  })
  .catch(err => {
    console.error("❌ Failed to load configuration:", err);
  });

// 2. Check MSAL instance
setTimeout(() => {
  console.log("\n2. Checking MSAL Authentication...");

  // Try to find MSAL instance in window
  const msalKeys = Object.keys(window).filter(k => k.toLowerCase().includes('msal'));
  if (msalKeys.length > 0) {
    console.log("✅ MSAL found in window:", msalKeys);
  } else {
    console.log("⚠️ MSAL not found in window object");
  }

  // Check localStorage/sessionStorage for MSAL data
  const storageKeys = [...Array(localStorage.length)].map((_, i) => localStorage.key(i))
    .filter(k => k && (k.includes('msal') || k.includes('login')));

  if (storageKeys.length > 0) {
    console.log("✅ MSAL data found in storage:", storageKeys.length, "items");
  } else {
    console.log("⚠️ No MSAL data in localStorage");
  }
}, 500);

// 3. Check for authentication state
setTimeout(() => {
  console.log("\n3. Checking Authentication State...");

  // Look for Blazor authentication state
  const authStateElement = document.querySelector('[data-auth-state]');
  if (authStateElement) {
    console.log("✅ Auth state element found");
  }

  // Check if user appears to be logged in (look for common indicators)
  const loginButton = document.body.textContent.toLowerCase().includes('log in');
  const logoutButton = document.body.textContent.toLowerCase().includes('log out');

  if (logoutButton) {
    console.log("✅ User appears to be logged in (logout button visible)");
  } else if (loginButton) {
    console.log("⚠️ User appears to be logged out (login button visible)");
  } else {
    console.log("⚠️ Cannot determine authentication state from UI");
  }
}, 1000);

// 4. Test API endpoint with credentials
setTimeout(() => {
  console.log("\n4. Testing API Endpoint...");

  fetch('/appsettings.Production.json')
    .then(r => r.json())
    .then(config => {
      const apiUrl = config.TimeClientBaseUrl;
      const testUrl = `${apiUrl}/api/TimePunch/lastpunch`;

      console.log(`Testing: ${testUrl}`);

      // This will fail if not authenticated, but we can see the error
      fetch(testUrl, {
        method: 'GET',
        credentials: 'include',
        headers: {
          'Accept': 'application/json'
        }
      })
      .then(response => {
        console.log(`Response Status: ${response.status} ${response.statusText}`);

        if (response.status === 401) {
          console.error("❌ 401 Unauthorized - Token not attached or invalid");
          console.log("Check:");
          console.log("  - ApiAuthorizationMessageHandler is registered");
          console.log("  - User is logged in");
          console.log("  - Token has correct scopes");
        } else if (response.status === 403) {
          console.error("❌ 403 Forbidden - Token valid but insufficient permissions");
        } else if (response.status === 200) {
          console.log("✅ API call succeeded!");
          return response.json();
        } else {
          console.log(`⚠️ Unexpected status: ${response.status}`);
        }
      })
      .then(data => {
        if (data) {
          console.log("✅ API Response:", data);
        }
      })
      .catch(err => {
        console.error("❌ API call failed:", err);
        console.log("This might be CORS, network, or authentication issue");
      });
    });
}, 1500);

// 5. Monitor network requests
setTimeout(() => {
  console.log("\n5. Network Request Monitoring");
  console.log("Check the Network tab in DevTools:");
  console.log("  1. Look for requests to the backend API");
  console.log("  2. Check Request Headers for 'Authorization: Bearer ...'");
  console.log("  3. Verify response status is 200, not 401/403");
  console.log("\nIf Authorization header is missing:");
  console.log("  - ApiAuthorizationMessageHandler may not be configured correctly");
  console.log("  - Check console for 'ApiAuthorizationMessageHandler configured' message");
  console.log("  - Verify user is authenticated");
}, 2000);

// 6. Summary
setTimeout(() => {
  console.log("\n=== Debug Summary ===");
  console.log("If you see issues above:");
  console.log("1. Configuration has PLACEHOLDERs → Re-deploy application");
  console.log("2. 401 Unauthorized → Check Authorization header in Network tab");
  console.log("3. No MSAL data → User may need to log in");
  console.log("4. CORS errors → Check backend API CORS configuration");
  console.log("\nFor more help, see: docs/AUTH_FIX_SUMMARY.md");
  console.log("=====================================\n");
}, 2500);

// Helper function to decode JWT (if you have a token)
window.decodeJWT = function(token) {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
      return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));

    const decoded = JSON.parse(jsonPayload);
    console.log("Token Claims:", decoded);
    console.log("Expires:", new Date(decoded.exp * 1000));
    console.log("Issued:", new Date(decoded.iat * 1000));
    console.log("Scopes:", decoded.scp || decoded.scope);
    return decoded;
  } catch (e) {
    console.error("Failed to decode JWT:", e);
  }
};

console.log("\n💡 Helper functions available:");
console.log("   - decodeJWT(token) - Decode and display JWT token claims");
