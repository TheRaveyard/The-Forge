param([string]$Revision)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent

function Read-ProjectFile([string]$Path) {
    if ($Revision) {
        $content = git -C $projectRoot show "${Revision}:$Path"
        if ($LASTEXITCODE -ne 0) { throw "Не удалось прочитать $Path из $Revision" }
        return $content -join "`n"
    }
    return [IO.File]::ReadAllText((Join-Path $projectRoot $Path))
}

$failures = [Collections.Generic.List[string]]::new()
function Check([bool]$Condition, [string]$Message) {
    if (!$Condition) { $failures.Add($Message) }
}

# Проверяем именно сохранённые ресурсы, используемые игровыми компонентами.
$input = Read-ProjectFile 'Assets/Archive/InputSystem_Actions.inputactions' | ConvertFrom-Json
$playerMap = $input.maps | Where-Object name -eq 'Player'
foreach ($name in @('Move', 'Look', 'Jump', 'Interact', 'Throw')) {
    Check (@($playerMap.actions | Where-Object name -eq $name).Count -eq 1) "Отсутствует или дублируется Player/$name"
}
foreach ($pair in @(@('Interact', '<Keyboard>/e'), @('Throw', '<Keyboard>/q'))) {
    Check (@($playerMap.bindings | Where-Object { $_.action -eq $pair[0] -and $_.path -eq $pair[1] }).Count -eq 1) "Неверная привязка $($pair[0])"
}
Check (($playerMap.actions | Where-Object name -eq 'Interact').interactions -eq '') 'Подбор требует удержания кнопки'

$scene = Read-ProjectFile 'Assets/Game/Scenes/MainScene.unity'
$interaction = [regex]::Match($scene, '(?s)--- !u!114 &1441761574\r?\n.*?(?=--- !u!)').Value
Check ($interaction.Contains('guid: 39da166136b107e4ead4aaec310a385a')) 'Нет ObjectInteraction в сцене'
Check ($scene.Contains('- component: {fileID: 1441761574}')) 'ObjectInteraction не включён в список компонентов Player'
Check ($interaction.Contains('m_GameObject: {fileID: 1441761569}')) 'ObjectInteraction подключён не к Player'
Check ($interaction.Contains('interactionCamera: {fileID: 330585545}')) 'Не подключена камера взаимодействия'
Check ($interaction.Contains('guid: 052faaac586de48259a63d0c4782560b')) 'Неверный Input Actions у ObjectInteraction'
$panel = [regex]::Match($scene, '(?s)--- !u!114 &1155073597\r?\n.*?(?=--- !u!)').Value
Check ($panel.Contains('m_RaycastTarget: 0')) 'Декоративная панель блокирует курсор'

$prefabs = @('Bronze', 'Copper', 'Gold') | ForEach-Object { "Assets/Game/Prefabs/$_.prefab" }
$prefabs += @('Bronze', 'Copper', 'Gold', 'Platinum', 'Silver', 'Steel') | ForEach-Object { "Assets/Blacksmith Pack/Prefabs/Small Tools/Ingots/$_ Ingot.prefab" }
foreach ($path in $prefabs) {
    $prefab = Read-ProjectFile $path
    foreach ($required in @('guid: 381edc0a10e15874eb3112da7fbb0531', 'Rigidbody:', 'BoxCollider:', 'm_IsKinematic: 0', 'm_UseGravity: 1', 'm_IsTrigger: 0', 'm_Interpolate: 1', 'm_CollisionDetection: 2')) {
        Check ($prefab.Contains($required)) "$path : отсутствует $required"
    }
    Check ($prefab -notmatch 'propertyPath: m_StaticEditorFlags\r?\n\s+value: [1-9]') "$path : предмет отмечен Static"
}
$movement = Read-ProjectFile 'Assets/Game/Scripts/Player/PlayerMovement.cs'
Check ($movement -notmatch 'Debug\.Log\(') 'Покадровый спам в PlayerMovement'
Check ($movement.Contains('Mouse.current.rightButton.isPressed')) 'Нет включения обзора по ПКМ'
if ($failures.Count) {
    $failures | ForEach-Object { Write-Output "FAIL: $_" }
    throw "Проверка Input System: ошибок $($failures.Count)"
}
Write-Output 'PASS: ввод, подключения сцены и девять подбираемых префабов. Физика и звук в Play не проверялись.'
