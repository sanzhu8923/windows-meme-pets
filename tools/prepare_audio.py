"""Convert AAC source clips to PCM WAV for the Windows media pipeline.
Original source files remain intact. Requires imageio-ffmpeg for rebuilding only.
"""
import json
import pathlib
import subprocess
import sys
import wave

import imageio_ffmpeg

release = pathlib.Path(sys.argv[1])
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
converted = 0
for role in ('SunPet', 'ShaolanPet', 'GoodThingPet'):
    folder = release / 'assets' / role
    manifest = folder / 'clips.json'
    clips = json.loads(manifest.read_text(encoding='utf-8'))
    for clip in clips:
        source = folder / 'Clips' / clip['file']
        if source.suffix.lower() == '.m4a':
            target = source.with_suffix('.wav')
            subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-nostdin', '-y',
                            '-i', str(source), '-vn', '-c:a', 'pcm_s16le', str(target)], check=True)
            clip['file'] = target.name
            with wave.open(str(target), 'rb') as audio:
                clip['duration'] = audio.getnframes() / audio.getframerate()
                if audio.getnframes() == 0:
                    raise RuntimeError(f'Empty audio: {target}')
            converted += 1
    manifest.write_text(json.dumps(clips, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'PCM conversion OK: {converted} AAC clips; original M4A files retained')
