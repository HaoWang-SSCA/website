# Fix: "Function language info isn't provided" Error

This error occurs during Azure Static Web Apps (SWA) deployment when the deployment engine cannot determine the runtime/language of the linked API (Azure Functions).

## Symptom
The GitHub Actions deployment step fails with:
`Cannot deploy to the function app because Function language info isn't provided.`

## Cause
Azure Static Web Apps requires the `apiRuntime` to be explicitly defined in the `staticwebapp.config.json` file, especially when using **Isolated Worker Model** for .NET functions or other specific runtimes.

## Solution

Ensure your `staticwebapp.config.json` file (usually located in `wwwroot` or the app's root) contains the `platform.apiRuntime` setting.

### For .NET 8.0 Isolated (Standard for this project):
```json
{
  "platform": {
    "apiRuntime": "dotnetisolated:8.0"
  },
  "navigationFallback": {
    "rewrite": "/index.html"
  }
}
```

### For other runtimes:
- **Node.js**: `node:18` (or your version)
- **Python**: `python:3.10`
- **.NET In-Process**: `dotnet:6.0`

## Implementation Checklist
1. Locate `staticwebapp.config.json`.
2. Add the `"platform": { "apiRuntime": "..." }` block.
3. Ensure the file is correctly copied to the build output directory used by the deployment action (e.g., `output/wwwroot`).
4. In your GitHub Action, ensure `api_location` points to the folder containing your compiled functions.

> [!TIP]
> If you are using .NET, make sure you use the `dotnetisolated` prefix if your project is using the Isolated Worker Model.
