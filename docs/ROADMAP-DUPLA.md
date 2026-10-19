# Vigilante 8 Classic / 2nd Offense — roadmap de desenvolvimento em dupla

> Projeto experimental de recompilação para PC. Usuário: testa no Windows com mídia própria e compartilha resultados. Assistente: investiga, implementa e organiza as alterações. Não publicar dados do jogo, mídias BIN/CUE, saves, caminhos pessoais ou dados privados no repositório.

**Branch de trabalho:** `feat/launcher-v1-hardening`  
**Regra:** um problema reproduzível por vez; antes/depois no mesmo mapa e configuração.  
**Estado inicial:** fork criado; a função **Issues** do fork está desabilitada (GitHub retorna 410). Os itens abaixo são rascunhos prontos para serem registrados como issues assim que habilitada.

## Backlog proposto (criar como GitHub Issues)

### 01 — [P0][Launcher] Corrigir seletor de BIN/CUE e entrada manual
- **Problema:** `FirstRunSetup.ChooseDisc` encerra em `Marshal.SizeOf<OpenFileName>()`.
- **Implementar:** `OPENFILENAMEW` marshalled corretamente, campo para colar caminho, tratamento de erro no UI/log, instalação sem seletor.
- **Aceite:** janela *Procurar* não cai; BIN/CUE válido instala; mídia ausente falha com mensagem e não com crash; argumento CLI continua operando.
- **Situação:** correção inicial implementada na branch; **sem compilação/teste Windows verificado**.

### 02 — [P1][Launcher] Launcher moderno de jogo, configurações e mods
- **Implementar:** tela **Jogar**, **Instalar/verificar dados**, **Configurações gráficas**, **Mods**, **Logs** e **Sair**.
- **Aceite:** preserva `settings.json`, `interface.ini` e saves; erros claros; UI coerente com teclado/mouse; configurações reversíveis.
- **Situação:** introduzido `--launcher` para abrir UI opt-in e botão *Play*. Launcher completo pendente.

### 03 — [P0][Build] Compilação reproduzível e diagnóstico no Windows
- **Contexto:** código gerado `reference-v8-2/generated/recompiled/*.cs` não é versionado. .NET 10 SDK, Python e geração a partir do jogo são exigidos.
- **Implementar:** bootstrap documentado, pré-checagens de dependências, coleta de logs, build Debug e Release, checklist de smoke.
- **Aceite:** pessoa sem a máquina do autor gera executável funcional usando mídia própria; nunca incluir arquivos comerciais no commit.

### 04 — [P0][Render] Diagnóstico visual A/B e capturas
- **Implementar:** mesmo mapa/câmera/carro com quatro combinações: 4:3 e 16:9; HD textures OFF e ON.
- **Coletar:** versão, hash, GPU/driver, mapa, captura, FPS, `OutputAspect`, `v8_latest.log` e proporção da imagem.
- **Aceite:** cada defeito tem passos claros de reprodução e evidência antes de alterar o renderizador.

### 05 — [P0][Render] Terreno desaparecendo / clipping 16:9
- **Contexto:** upstream `notes/v82_widescreen_terrain_investigation.md` e `notes/v82_near_object_side_clipping.md`.
- **Investigar:** frustum, culling de terreno, recorte nativo, profundidade dos triângulos e retorno aos menus.
- **Aceite:** sem faixas/buracos em Route 66 e mais dois mapas; sem regressão em 4:3 e cenas/menu.

### 06 — [P0][Textures] Atlas e texturas desaparecendo/piscando
- **Investigar:** presença das DDS, hit/miss no atlas, `CLUT/tpage`, UV, cache por cena, mipmaps, substituições 2x/4x.
- **Aceite:** texturas estáveis com HD OFF/ON no mesmo mapa; fallback seguro quando faltarem recursos; logs objetivos.

### 07 — [P1][Quality] Escala interna e upscaling
- **Contexto:** `ViewConfig.InternalResolutionScale` limitado a 4x; resolução de saída é diferente.
- **Implementar:** medir limite real antes de alterar, nitidez configurável e evitar blur, preservando HUD.
- **Aceite:** capturas A/B, opção para desligar, benchmark 1080p/1440p, sem distorções.

### 08 — [P1][Shaders] Pós-processamento GLSL configurável
- **Local:** `PresentationRenderer.cs` já possui passes de upscale e FXAA.
- **Implementar em ordem:** sharpen moderado, color grading, bloom leve, presets Off/Subtle/Enhanced, fallback de shader.
- **Aceite:** pass-through OFF sem diferença visível, UI legível, desempenho medido e nenhuma regressão de gameplay.

### 09 — [P2][Shaders] Fog, iluminação, água, partículas e materiais
- **Local:** `GlShaders.cs`, `EnhancedGlBackend.cs`.
- **Investigar:** normais reais, profundidade, cor do ambiente, separação de tipos de geometria, transparência.
- **Aceite:** sem normals inventadas, zero mudança na lógica do PS1, capturas com e sem efeitos e sem mascarar os bugs P0.

### 10 — [P1][UX/QA] Presets, documentar e validar releases
- **Implementar:** Compatibilidade / Clássico melhorado / Qualidade, explicação 4:3 do menu e 16:9 no gameplay, exportação de diagnóstico (remover caminhos pessoais).
- **Aceite:** pelo menos 3 mapas, som, controles, salvamento, menus, alternância 16:9, texturas e smoke de 10 minutos.

## Protocolo de teste local
1. Nunca substituir diretamente sua instalação v1.0 funcional. Copiar para uma pasta de teste.
2. Testar com arquivos BIN/CUE completos obtidos legalmente.
3. Identificar jogo, mapa, veículo, resolução, widescreen e HD textures.
4. Reproduzir um problema com um único conjunto de variáveis alterado.
5. Enviar descrição + screenshot do gameplay + trecho do `v8_latest.log`.
6. Validar um commit por vez. Só promover à `main` depois da aprovação manual.

## Estado das mudanças do launcher
- [x] Branch `feat/launcher-v1-hardening` criada.
- [x] Substituído marshaling de campos gerenciados por ponteiros de `OPENFILENAMEW`.
- [x] Entrada de caminho manual + mensagem de erros e progresso.
- [x] Estilo visual inicial e modo opt-in `--launcher` com botão *Play* quando instalado.
- [ ] Compilação Debug do host com fontes geradas.
- [ ] Teste real do seletor em Windows x64.
- [ ] Teste de instalação e de relançamento.
- [ ] UX de configurações e mods integrada.
- [ ] PR revisada / merge após validação.

## Referências
- Upstream: https://github.com/GTTeancum/Vigilante-8-recomp
- Report de crash: https://github.com/GTTeancum/Vigilante-8-recomp/issues/2
- Report de crash aberto: https://github.com/GTTeancum/Vigilante-8-recomp/issues/3
- Código: `tools/recompone-reference/RecompOne.Runtime/Host/FirstRunSetup.cs`
- Build: `README.md`, `tools/recompone-v8-2/README.md`, `reference-v8-2/README.md`
