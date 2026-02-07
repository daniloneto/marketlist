#!/usr/bin/env pwsh
# Script de teste do Chat Assistant

# Aguarde a API estar pronta
Write-Host "Aguardando API em http://localhost:5000..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# URL base
$baseUrl = "http://localhost:5000/api/chat"

# Test 1: Get available tools
Write-Host "`n📋 Teste 1: Obter ferramentas disponíveis" -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/tools" -Method GET
    Write-Host "✅ Sucesso! Ferramentas:" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ForEach-Object { Write-Host "  - $_" }
}
catch {
    Write-Host "❌ Erro: $_" -ForegroundColor Red
}

# Test 2: Send simple message
Write-Host "`n💬 Teste 2: Enviar mensagem de teste" -ForegroundColor Cyan
try {
    $body = @{
        message = "Quais são minhas últimas listas de compras?"
        conversationHistory = @()
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "$baseUrl/message" -Method POST -ContentType "application/json" -Body $body
    $result = $response.Content | ConvertFrom-Json
    Write-Host "✅ Resposta:" -ForegroundColor Green
    Write-Host $result.message
}
catch {
    Write-Host "❌ Erro: $_" -ForegroundColor Red
}

# Test 3: Stream message (simulado)
Write-Host "`n🌊 Teste 3: Mensagem com stream" -ForegroundColor Cyan
Write-Host "Abra o frontend em http://localhost:5173 para ver o chat em ação!" -ForegroundColor Yellow

Write-Host "`n✨ Testes concluídos!" -ForegroundColor Green
