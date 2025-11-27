# Quick Start Guide - JWT Authentication & Performance Monitoring

## ?? Getting Started in 5 Minutes

### Step 1: Run the Application

```bash
cd api-aggregator.WebAPI
dotnet run
```

### Step 2: Login and Get Token

```bash
curl -X POST https://localhost:5001/api/authentication/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin123"
  }'
```

**Copy the token from the response!**

### Step 3: Use the Token

Replace `YOUR_TOKEN_HERE` with the token from Step 2:

```bash
curl -X GET "https://localhost:5001/api/aggregation?sources=Users" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Step 4: Watch Performance Logs

In your terminal where the app is running, look for:

```
[10:30:01 INF] Performance Monitoring Service started. Checking every 1 minutes.
[10:31:01 DBG] API 'Users' performance is normal: Recent=145.23ms, Overall=156.78ms
```

## ?? Demo Users Cheat Sheet

| Username | Password    | Roles       |
|----------|-------------|-------------|
| admin    | admin123    | Admin, User |
| user     | user123     | User        |
| readonly | readonly123 | ReadOnly    |

## ?? All Available Endpoints

### Authentication Endpoints (No token required)

```bash
# Login
POST /api/authentication/login
Body: {"username": "admin", "password": "admin123"}

# Verify token (token required but for testing)
GET /api/authentication/me
Header: Authorization: Bearer {token}
```

### Protected Endpoints (Token required)

```bash
# Get aggregated data (POST)
POST /api/aggregation/aggregate
Header: Authorization: Bearer {token}
Body: {"sources": ["Users"], "maxItems": 10}

# Get aggregated data (GET - simpler)
GET /api/aggregation?sources=Users&maxItems=10
Header: Authorization: Bearer {token}

# Get all statistics
GET /api/statistics
Header: Authorization: Bearer {token}

# Get statistics for specific API
GET /api/statistics/Users
Header: Authorization: Bearer {token}

# Reset statistics
DELETE /api/statistics
Header: Authorization: Bearer {token}
```

## ??? Common Tasks

### Change Token Expiration

Edit `appsettings.json`:

```json
{
  "Jwt": {
    "ExpirationMinutes": 120  // Change to desired minutes
  }
}
```

### Change Performance Monitoring Interval

Edit `PerformanceMonitoringService.cs`:

```csharp
private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes
```

### Change Anomaly Threshold

Edit `PerformanceMonitoringService.cs`:

```csharp
private readonly double _anomalyThresholdPercentage = 75.0; // 75% increase threshold
```

## ?? Troubleshooting

### "401 Unauthorized" Error

**Problem:** Token is missing or invalid

**Solutions:**
1. Make sure you're including the `Authorization: Bearer {token}` header
2. Check that your token hasn't expired
3. Login again to get a fresh token

### "JWT authentication is not configured properly"

**Problem:** Secret key is too short or missing

**Solution:** In `appsettings.json`, ensure `Jwt.SecretKey` is at least 32 characters:

```json
{
  "Jwt": {
    "SecretKey": "ThisIsASecretKeyForJwtTokenGeneration32CharactersMinimum!"
  }
}
```

### Performance Monitoring Not Logging

**Problem:** Log level is too high

**Solution:** Set logging level in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "api_aggregator.Services.BackgroundServices": "Debug"
    }
  }
}
```

### No Performance Anomalies Detected

**Problem:** Not enough data or performance is actually normal

**Solutions:**
1. Make more API calls to generate data
2. Wait at least 5-10 minutes for metrics to accumulate
3. The APIs might actually be performing well! ??

## ?? Bash Script for Easy Testing

Save this as `test-api.sh`:

```bash
#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

BASE_URL="https://localhost:5001"

echo -e "${BLUE}=== API Aggregator Test Script ===${NC}\n"

# Step 1: Login
echo -e "${GREEN}1. Logging in as admin...${NC}"
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/authentication/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}')

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')
echo "Token: ${TOKEN:0:50}..."
echo ""

# Step 2: Get aggregated data
echo -e "${GREEN}2. Fetching aggregated data...${NC}"
curl -s -X GET "$BASE_URL/api/aggregation?sources=Users&maxItems=3" \
  -H "Authorization: Bearer $TOKEN" | jq '.'
echo ""

# Step 3: Get statistics
echo -e "${GREEN}3. Fetching statistics...${NC}"
curl -s -X GET "$BASE_URL/api/statistics" \
  -H "Authorization: Bearer $TOKEN" | jq '.'
echo ""

# Step 4: Verify token
echo -e "${GREEN}4. Verifying token...${NC}"
curl -s -X GET "$BASE_URL/api/authentication/me" \
  -H "Authorization: Bearer $TOKEN" | jq '.'
echo ""

echo -e "${BLUE}=== Test Complete ===${NC}"
```

Make it executable and run:

```bash
chmod +x test-api.sh
./test-api.sh
```

## ?? Learn More

- **Full Documentation:** See [FEATURES.md](FEATURES.md)
- **Project Overview:** See [README.md](README.md)
- **Implementation Details:** See [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)

## ? Pro Tips

1. **Use Postman or Thunder Client** for easier testing with a GUI
2. **Copy token to environment variable:**
   ```bash
   export TOKEN="eyJhbGciOiJIUzI1..."
   curl -H "Authorization: Bearer $TOKEN" https://localhost:5001/api/aggregation
   ```
3. **Watch logs in real-time:**
   ```bash
   dotnet run --project api-aggregator.WebAPI | grep "Performance\|ANOMALY"
   ```
4. **JWT Debugger:** Paste your token at [jwt.io](https://jwt.io) to see the claims

---

**Happy Coding! ??**
