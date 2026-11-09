<!-- Bridge managed wiki source -->
# Подключение Codex через MCP

Полный маршрут с точными командами, TOML, credentials и восстановлением после ошибок — [Player → MCP → Codex](../../runtime/mcp.md). Не пропускайте publish gateway, запуск отдельного Player, передачу environment и новую сессию клиента.

MCP gateway — отдельный .NET8 процесс на официальном SDK2.4.0. stdio три tools: bridge_discover, bridge_query, bridge_execute. Discovery содержит реальные формальные схемы разрешённых C# команд; query использует Core QuerySpec, а execute принимает зарегистрированное имя/arguments. Никакого eval, доступа к Unity source files или произвольной файловой системе.

```powershell
./tools/Verify-RuntimeUnity.ps1 -Mode WindowsBuild
dotnet publish samples/Bridge.Runtime.Mcp.Sample -c Release -o work/mcp-gateway
./tools/Verify-RuntimeMcp.ps1
```

Ожидается MCP_PLAYER_E2E_PASS: настоящий собранный Player, отдельный gateway и официальный SDK client создают3/перемещают/удаляют/читаютlive2, проверяют permissions/auth/owner thread. Машинный compatible-client E2E фактически пройден. **Личный Codex connected E2E не заявляется**; global config не изменён автоматически. [Готовая секция конфигурации](../../../samples/Bridge.Runtime.Mcp.Sample/codex-config.example.toml) содержит env_vars, а не секрет.

В новой сессии Codex попросите выполнить задание трёх кубов из полного guide. `codex mcp list` подтверждает только регистрацию, не выполнение команд. При timeout не повторяйте mutation автоматически: отмена без rollback. Для Read используйте отдельный server-side credential; JSON permission не принимается.
