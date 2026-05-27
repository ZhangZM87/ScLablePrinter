from pathlib import Path
from codecs import decode
path = Path(r"C:\Users\MC\Desktop\软件产品\开发包\赛创标签打印机\SDK（开发包..程序\SDK\C#\C#\C#版例程 20230523 Win10 21H2及Win11 USB\GprinterDemo230518\bin\Debug\test1.txt")
text = path.read_text('utf-8')
clean = ''.join(ch for ch in text if not ch.isspace())
print('clean len', len(clean))
print('odd', len(clean) % 2)
print('first100', clean[:100])
print('allhex', all(ch in '0123456789abcdefABCDEF' for ch in clean))
