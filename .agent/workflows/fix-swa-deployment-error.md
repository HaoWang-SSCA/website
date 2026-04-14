---
description: Fix "Function language info isn't provided" deployment error
---

This workflow helps detect and fix the missing `apiRuntime` configuration in `staticwebapp.config.json`.

### Steps

1. **Locate `staticwebapp.config.json`**
   Search for the configuration file in your project. Common locations:
   - `src/YourUIProject/wwwroot/staticwebapp.config.json`
   - Project Root: `./staticwebapp.config.json`

2. **Check for `apiRuntime`**
   Open the file and check if it has a `platform` object with `apiRuntime`.

3. **Apply Fix**
   If missing, add the following configuration (adjusted for your runtime, e.g., `dotnetisolated:8.0` for .NET 8 Isolated):

```json
{
  "platform": {
    "apiRuntime": "dotnetisolated:8.0"
  }
}
```

4. **Verify GitHub Workflow**
   Ensure your GitHub Actions workflow (e.g., `.github/workflows/deploy.yml`) copies this file to the artifact location before deployment.

Example copy command in workflow:
```bash
cp src/YourUIProject/wwwroot/staticwebapp.config.json output/wwwroot/
```
