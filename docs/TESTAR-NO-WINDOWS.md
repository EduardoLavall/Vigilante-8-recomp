# Como testar a branch de launcher no Windows (dupla)

**Status:** código enviado ao GitHub, mas ainda NÃO compilado/testado numa máquina Windows. Nada neste documento altera sua instalação existente automaticamente.

## 1. Não estrague a instalação que já roda
- Guarde a pasta da release v1.0 que funciona.
- Faça testes em outra pasta com dados separados.
- Não adicione imagens BIN/CUE, `game_data`, saves ou mods protegidos por direitos autorais ao Git.

## 2. Obtenha a branch
Abra PowerShell numa pasta de projetos:

```powershell
git clone --branch feat/launcher-v1-hardening https://github.com/EduardoLavall/Vigilante-8-recomp.git
cd Vigilante-8-recomp
git log -1 --oneline
```

## 3. Verifique os requisitos antes de tentar compilar
```powershell
dotnet --version
python --version
Test-Path .\reference-v8-2\generated\recompiled\main.cs
```

O arquivo `main.cs` gerado **não vem no GitHub**. A build completa precisa da etapa de geração descrita no README original a partir de uma cópia legítima do **Second Offense USA (SLUS-00868)**.

O processo upstream começa assim (substitua o caminho BIN pelos seus arquivos):

```powershell
python tools/extract_psx_iso.py "<caminho para seu segundo jogo>.bin" V8_2_WORK/disc --clean
python tools/recompone-v8-2/prepare_reference.py
```

**Atenção:** `prepare_reference.py` prepara o projeto, mas a geração das fontes C# pelo RecompOne é outra etapa. Se `Test-Path` continuar `False`, ainda **não existe uma build testável**; não tente resolver isso copiando arquivos comerciais para o repo. Não afirmamos que o pipeline inteiro foi reproduzido neste fork.

## 4. Compile quando as fontes geradas estiverem prontas
```powershell
dotnet build tools/recompone-v8-2/reference-host/Vigilante82PC.csproj -c Debug
```

O executável de Debug tem console para diagnóstico. O build Release publicado pelo upstream é `WinExe` e pode não mostrar nada no PowerShell apesar de gravar logs.

## 5. Testes funcionais da alteração
O código desta branch adiciona `--launcher`. Depois da compilação, aponte o comando para o executável **recém-compilado** (não para a v1.0 oficial). Exemplos, se a build estiver no caminho padrão:

```powershell
.\tools\recompone-v8-2\reference-host\bin\Debug\net10.0\Vigilante82PC.exe --launcher
```

Valide, nesta ordem:
- [ ] Clicar em **BROWSE FILES...** não fecha o aplicativo.
- [ ] Colar um caminho completo .CUE em **Disc image path** e clicar **INSTALL FROM PATH** exibe progresso.
- [ ] Caminho inválido mostra mensagem na própria interface; ela continua aberta.
- [ ] Selecionar BIN com CUE correspondente funciona; BIN isolado mostra erro útil.
- [ ] Depois de instalada a mídia, abrir `--launcher` mostra **PLAY SECOND OFFENSE**.
- [ ] O botão *Play* abre o jogo.
- [ ] A execução normal sem `--launcher` continua igual à versão original.

## 6. Diagnóstico visual no jogo
No menu OPTIONS > Video, compare o mesmo mapa e câmera nestas combinações:
1. Widescreen OFF, High-res textures OFF
2. Widescreen OFF, High-res textures ON
3. Widescreen ON, High-res textures OFF
4. Widescreen ON, High-res textures ON

Em cada teste, registrar:
- Mapa / veículo / momento do defeito.
- Resolução e configurações MSAA, mipmaps e LOD.
- Uma screenshot DURANTE a partida e as linhas relevantes de `v8_latest.log`.
- O que sumiu: terreno, objetos, veículo, textura, sombra, HUD ou outro.

**Não publique logs brutos sem revisar caminhos `C:\Users\...`, nomes de conta ou dados privados.**

## 7. Feedback
Escreva os resultados no chat ou, quando Issues estiver habilitado, em uma issue específica. A branch ainda precisa passar por compilação e testes locais antes de mergear na `main`.
