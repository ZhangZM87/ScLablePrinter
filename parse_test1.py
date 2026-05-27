from pathlib import Path
import codecs

path = Path(r"C:\Users\MC\Desktop\软件产品\开发包\赛创标签打印机\SDK（开发包..程序\SDK\C#\C#\C#版例程 20230523 Win10 21H2及Win11 USB\GprinterDemo230518\bin\Debug\test1.txt")
text = path.read_text('utf-8')
print('raw length=', len(text))
print('ascii hex content=', all(ch.isspace() or ch in '0123456789abcdefABCDEF' for ch in text))
clean = ''.join(ch for ch in text if not ch.isspace())
print('clean length=', len(clean))
print('clean mod2=', len(clean) % 2)
bytes_data = codecs.decode(clean, 'hex')
print('decoded bytes length=', len(bytes_data))
print('sample decoded text head=')
print(bytes_data[:400].decode('latin1', errors='replace'))
print('contains non-print ratio=', sum(1 for b in bytes_data[:500] if b < 0x20 and b not in (0x09,0x0A,0x0D) or b==0x7F) / min(500, len(bytes_data)))
print('first 300 bytes hex=', bytes_data[:300].hex().upper())
