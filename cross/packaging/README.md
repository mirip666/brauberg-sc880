# Linux packages

Скрипты собирают пакеты из готовой self-contained публикации:

```bash
dotnet publish cross/Sc880.Desktop/Sc880.Desktop.csproj \
  -c Release -r linux-x64 --self-contained true -o cross/publish/linux-x64

export VERSION=1.1.0
export PUBLISH=cross/publish/linux-x64
export OUT=dist/linux
```

| Скрипт | Результат | Требования |
| --- | --- | --- |
| `linux/build-tar.sh` | `brauberg-sc880-$VERSION-linux-x64.tar.gz` | `tar` |
| `linux/build-deb.sh` | `brauberg-sc880_${VERSION}_amd64.deb` | `dpkg-deb` |
| `linux/build-rpm.sh` | `brauberg-sc880-$VERSION-1.x86_64.rpm` | `rpmbuild` |
| `linux/build-appimage.sh` | `brauberg-sc880-$VERSION-x86_64.AppImage` | `curl`, интернет для appimagetool |

Arch-пакет собирается из опубликованного tar.gz:

```bash
cp cross/packaging/arch/PKGBUILD ~/build/brauberg-sc880/
cp dist/linux/brauberg-sc880-1.1.0-linux-x64.tar.gz ~/build/brauberg-sc880/
cd ~/build/brauberg-sc880 && makepkg -f
```

Все пакеты ставят приложение в `/usr/lib/brauberg-sc880`, создают ссылку
`/usr/bin/brauberg-sc880`, пункт меню и иконку. CI-воркфлоу
`.github/workflows/linux.yml` собирает их автоматически и прикладывает
к артефактам сборки.
