# Vigilante 8 Classic — launcher para Windows

Este é um **launcher independente** para a release v1.0 de `Vigilante8PC.exe`. Não precisa dos C# gerados pelo RecompOne para compilar ou rodar. Não inclui arquivos do jogo e não acessa a rede.

## Recursos da primeira versão
- Tela separada com **Play**, status da instalação e importação de mídia (CUE) usando o seletor normal do Windows.
- Configurações gráficas persistentes: resolução, widescreen, texturas HD, smoothing, fullscreen, draw distance, mipmaps, FXAA, MSAA e LOD.
- Atualização **somente** da seção `[RecompOne]` do `interface.ini`; se existir, um backup único `interface.ini.before-launcher.bak` é criado antes de modificar.
- Acesso ao log e à pasta do jogo.
- Usa a opção `--extract-loose` da release v1.0 para instalar uma cópia legal do **Vigilante 8: 2nd Offense USA (SLUS-00868)**. Não inclui, baixa ou altera o jogo original.
- Não substitui o executável: chama a cópia `Vigilante8PC.exe` encontrada ao lado do launcher.

## Como baixar a versão compilada pelo GitHub Actions
1. Acesse [Actions do fork](https://github.com/EduardoLavall/Vigilante-8-recomp/actions).
2. Abra uma execução concluída com sucesso do workflow **Runtime build (no game assets)**, da branch `feat/launcher-v1-hardening`.
3. Em **Artifacts**, escolha `Vigilante8Launcher-win-x64` e extraia o ZIP.
4. **Copie só `Vigilante8Launcher.exe` para a mesma pasta de `Vigilante8PC.exe`** da sua release v1.0 já instalada.
5. Execute `Vigilante8Launcher.exe`; ajustes vão para o `interface.ini` dessa pasta.

O artifact só fica disponível se o job *portable-launcher* tiver compilado e publicado com sucesso; ele pode não existir se o workflow falhou.

## Como compilar no Windows (.NET 10 SDK)
```powershell
git clone --branch feat/launcher-v1-hardening https://github.com/EduardoLavall/Vigilante-8-recomp.git
cd Vigilante-8-recomp
dotnet publish tools/standalone-launcher/Vigilante8Launcher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o dist/launcher
```
Copie `dist/launcher/Vigilante8Launcher.exe` para a pasta existente do jogo.

## Cuidados
- O launcher é experimental e ainda precisa de verificação local em Windows x64. Um CI verde garante compilação, não compatibilidade funcional ou segurança absoluta.
- Faça backup de `interface.ini`, `settings.json`, saves e `game_data` antes do teste. Não misture pastas de `0.9b` e `v1.0`.
- **Widescreen é somente para gameplay**: menu e vídeos continuam 4:3 por design.
- Resolução de **saída** não equivale à resolução **interna** ou à qualidade real dos modelos/texturas.
- Se alguma textura desaparece, compare com o High-res textures desligado no mesmo mapa.
- Se a tela do launcher não abrir ou o jogo falhar, abra `v8_latest.log` e compartilhe somente trechos revisados (não exponha diretórios pessoais).

## Testes de aceitação
- [ ] Launcher inicia fora do RecompOne sem precisar de BIN/CUE ou fontes geradas.
- [ ] Identifica `Vigilante8PC.exe` e `game_data` válida.
- [ ] Gráficos são salvos e aplicados ao executar o jogo, sem apagar outras seções do INI.
- [ ] Play abre o jogo.
- [ ] Importar a imagem CUE funciona em cópia limpa da v1.0.
- [ ] Botões log/pasta funcionam.
- [ ] Não há efeitos colaterais em saves ou mods.
