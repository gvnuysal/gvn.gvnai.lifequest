#!/usr/bin/env bash
# Gvn.GvnFramework paketlerini repodaki yerel NuGet kaynağına kopyalar.
# Kullanım: scripts/update-framework-packages.sh [artifacts-klasörü]
# Sürüm yükseltirken Directory.Packages.props içindeki GvnFrameworkVersion da güncellenmelidir.
set -euo pipefail

source_dir="${1:-../Gvn.GvnFramework/artifacts}"
target_dir="$(cd "$(dirname "$0")/.." && pwd)/packages/gvnframework"

if ! ls "$source_dir"/Gvn.GvnFramework.*.nupkg >/dev/null 2>&1; then
  echo "Paket bulunamadı: $source_dir (önce framework reposunda 'dotnet pack -o artifacts' çalıştırın)" >&2
  exit 1
fi

mkdir -p "$target_dir"
cp "$source_dir"/Gvn.GvnFramework.*.nupkg "$target_dir"/
echo "Kopyalandı → $target_dir"
ls -1 "$target_dir"
echo "Not: aynı sürüm numarasıyla yeniden paketlediyseniz 'dotnet nuget locals global-packages --clear' gerekebilir."
