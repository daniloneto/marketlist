#!/usr/bin/env pwsh
# Script simples de teste da API de chat

Write-Host "Testando API de Chat..." -ForegroundColor Cyan

# Test 1: Get tools
Write-Host "`nTestando GET /api/chat/tools..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/chat/tools" -Method GET -UseBasicParsing
    $tools = $response.Content | ConvertFrom-Json
    Write-Host "Sucesso! Ferramentas disponíveis: $($tools.Count)" -ForegroundColor Green
}
catch {
    Write-Host "Erro: $_" -ForegroundColor Red
}

# Test 2: Send message
Write-Host "`nTestando POST /api/chat/message..." -ForegroundColor Yellow
try {
    $body = @{
        message = "Ola"
        conversationHistory = @()
    } | ConvertTo-Json
    
    Write-Host "Enviando: $body" -ForegroundColor Gray
    
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/chat/message" -Method POST -ContentType "application/json" -Body $body -UseBasicParsing
    $result = $response.Content | ConvertFrom-Json
    Write-Host "Sucesso! Resposta: $($result.message)" -ForegroundColor Green
}
catch {
    Write-Host "Erro ao enviar mensagem: $_" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
}

Write-Host "`nTestes concluidos!" -ForegroundColor Green
