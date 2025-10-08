$body = '{"url":"https://www.youtube.com/watch?v=dQw4w9WgXcQ","priority":"High"}'
$headers = @{
    'Content-Type' = 'application/json'
    'Authorization' = 'Bearer mock-token'
}
$response = Invoke-RestMethod -Uri 'https://localhost:62787/api/v1/videos/ingest' -Method POST -Body $body -Headers $headers -SkipCertificateCheck
$response | ConvertTo-Json
