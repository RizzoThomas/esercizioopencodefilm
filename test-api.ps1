# Test script for TaskFlow API endpoints
Write-Host "Starting API tests..." -ForegroundColor Green

# Wait for API to start
Start-Sleep -Seconds 3

# Test 1: Create a new task
Write-Host "`n1. Creating a new task..." -ForegroundColor Cyan
$taskBody = @{
    title = "Test Task"
    description = "This is a test task for scoring"
    status = "open"
    dueDate = (Get-Date).AddDays(2).ToString("yyyy-MM-ddTHH:mm:ss")
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/tasks" -Method Post -Body $taskBody -ContentType "application/json"
$taskId = $response.id
Write-Host "Created task with ID: $taskId" -ForegroundColor Green
Write-Host "Task: $($response | ConvertTo-Json)" -ForegroundColor Gray

# Test 2: Score the task
Write-Host "`n2. Computing priority score for task $taskId..." -ForegroundColor Cyan
$scoreResponse = Invoke-RestMethod -Uri "http://localhost:5000/tasks/$taskId/score" -Method Post
Write-Host "Score: $($scoreResponse.priorityScore)" -ForegroundColor Green
Write-Host "Response: $($scoreResponse | ConvertTo-Json)" -ForegroundColor Gray

# Test 3: Get the task to verify score was updated
Write-Host "`n3. Retrieving task to verify score..." -ForegroundColor Cyan
$updatedTask = Invoke-RestMethod -Uri "http://localhost:5000/tasks/$taskId" -Method Get
Write-Host "Updated Priority Score: $($updatedTask.priorityScore)" -ForegroundColor Green

# Test 4: Create a task from natural language
Write-Host "`n4. Creating task from natural language..." -ForegroundColor Cyan
$nlBody = @{
    text = "Create a task: Fix the login bug by 2026-03-21"
} | ConvertTo-Json

$nlResponse = Invoke-RestMethod -Uri "http://localhost:5000/tasks/nl" -Method Post -Body $nlBody -ContentType "application/json"
Write-Host "Created NL task with ID: $($nlResponse.id)" -ForegroundColor Green
Write-Host "Task: $($nlResponse | ConvertTo-Json)" -ForegroundColor Gray

# Test 5: List all tasks
Write-Host "`n5. Listing all tasks..." -ForegroundColor Cyan
$allTasks = Invoke-RestMethod -Uri "http://localhost:5000/tasks" -Method Get
Write-Host "Total tasks: $($allTasks.Count)" -ForegroundColor Green

Write-Host "`nAll tests completed successfully!" -ForegroundColor Green
