#!/usr/bin/env python3
"""Package the exact tested static output with stable paths and archive metadata."""
import gzip
from pathlib import Path
import sys
import tarfile

source = Path(sys.argv[1] if len(sys.argv) > 1 else 'artifacts/browser/wwwroot').resolve()
destination = Path(sys.argv[2] if len(sys.argv) > 2 else 'artifacts/stm32-browser.tar.gz').resolve()
if not (source / 'index.html').is_file():
    raise SystemExit('Publish the browser app before packaging it.')
destination.parent.mkdir(parents=True, exist_ok=True)
with destination.open('wb') as output, gzip.GzipFile(filename='', fileobj=output, mode='wb', mtime=0) as compressed:
    with tarfile.open(fileobj=compressed, mode='w') as archive:
        for path in sorted(source.rglob('*')):
            if path.is_symlink():
                raise SystemExit(f'Refusing symlink: {path}')
            if not path.is_file():
                continue
            info = archive.gettarinfo(str(path), arcname=path.relative_to(source).as_posix())
            info.uid = info.gid = info.mtime = 0
            info.uname = info.gname = ''
            info.mode = 0o644
            with path.open('rb') as contents:
                archive.addfile(info, contents)
print(destination)
