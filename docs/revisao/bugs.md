# Bugs encontrados e corrigidos

| # | Sintoma | Causa | Arquivo | Correção |
|---|---|---|---|---|
| 1 | Nenhuma build compilava ("scripts had compiler errors") | Ferramenta de editor `AutomaticLODTools` (usa `UnityEditor`) estava na pasta de scripts do jogo | `Assets/Scripts/Optimization/AutomaticLODTools.cs` | Movida para `Assets/Editor/Optimization/` (mesmo GUID) |
