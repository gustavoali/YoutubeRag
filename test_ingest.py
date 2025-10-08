import urllib.request
import urllib.error
import json
import ssl

# Create SSL context that doesn't verify certificates
ssl_context = ssl._create_unverified_context()

# Prepare request
url = "https://localhost:62787/api/v1/videos/ingest"
headers = {
    "Content-Type": "application/json",
    "Authorization": "Bearer mock-token"
}
data = {
    "url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
    "priority": 2  # High = 2
}

# Make request
req = urllib.request.Request(
    url,
    data=json.dumps(data).encode('utf-8'),
    headers=headers,
    method='POST'
)

try:
    with urllib.request.urlopen(req, context=ssl_context) as response:
        result = json.loads(response.read().decode('utf-8'))
        print(json.dumps(result, indent=2))
except urllib.error.HTTPError as e:
    print(f"HTTP Error {e.code}: {e.reason}")
    error_body = e.read().decode('utf-8')
    print("\n=== ERROR RESPONSE BODY ===")
    try:
        error_json = json.loads(error_body)
        print(json.dumps(error_json, indent=2))
    except:
        print(error_body)
except Exception as e:
    print(f"Error: {e}")
