# Bugs encontrados e corrigidos

| # | Sintoma | Causa | Arquivo | Correção |
|---|---|---|---|---|
| 1 | Nenhuma build compilava ("scripts had compiler errors") | Ferramenta de editor `AutomaticLODTools` (usa `UnityEditor`) estava na pasta de scripts do jogo | `Assets/Scripts/Optimization/AutomaticLODTools.cs` | Movida para `Assets/Editor/Optimization/` (mesmo GUID) |
| 2 | Na build, 3 erros ao entrar na fase: `Not allowed to access vertices/normals/uv4 on mesh Combined Mesh (root: scene)` | `Object_4` tem contorno (QuickOutline), que lê a malha no `Awake`, e estava marcado como static batching; na build a malha vira o "Combined Mesh" não legível | `Assets/Scenes/Principal.unity` (`Object_4`) | Static batching desligado só nos objetos com `Outline` (teste `OutlineStaticCheck`) |
| 3 | A pilha de bateria da fase caía ao iniciar (de y 1,08 até abaixo do piso) e ficava inalcançável: sem como recarregar a câmera | `Rigidbody` da pilha com gravidade e não cinemático; passava pelo vão do piso | `Assets/Prefabs/Battery/Battery (1).prefab`, `Assets/Scenes/Principal.unity` (`Object_4`) | `Rigidbody` cinemático e sem gravidade (o save continua posicionando pelo `Rigidbody`); teste `BatteryHeat` confere que a pilha fica no lugar |
