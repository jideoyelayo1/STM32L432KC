#!/usr/bin/env python3
"""Serve the published lab at its real subdirectory, only on the loopback interface."""
import argparse,http.server,pathlib,urllib.parse
p=argparse.ArgumentParser();p.add_argument('--port',type=int,default=4177);args=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[1]/'artifacts/browser/wwwroot'
if not (root/'index.html').is_file():raise SystemExit('Publish src/Stm32Emulator.Web into artifacts/browser first.')
class Handler(http.server.SimpleHTTPRequestHandler):
 def __init__(self,*args,**kwargs):super().__init__(*args,directory=str(root),**kwargs)
 def translate_path(self,path):
  path=urllib.parse.urlsplit(path).path
  if path in ['/demos/stm32','/demos/stm32/']:path='/index.html'
  elif path.startswith('/demos/stm32/'):path=path[len('/demos/stm32'):]
  else:path='/__missing_route__'
  return super().translate_path(path)
 def log_message(self,*args):pass
http.server.ThreadingHTTPServer(('127.0.0.1',args.port),Handler).serve_forever()
