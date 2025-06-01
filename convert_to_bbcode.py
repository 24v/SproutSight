#!/usr/bin/env python3
import re
import sys

if len(sys.argv) != 4:
    print("Usage: python3 convert_to_bbcode.py <input_file> <output_file> <github_raw_base>")
    sys.exit(1)

input_file = sys.argv[1]
output_file = sys.argv[2]
github_raw_base = sys.argv[3]

try:
    with open(input_file, 'r') as f:
        content = f.read()

    # Process images first to avoid conflicts with other patterns
    def replace_image(match):
        alt_text = match.group(1)
        image_path = match.group(2)
        # If the image path doesn't start with http, prepend the GitHub raw base URL
        if not image_path.startswith(('http://', 'https://')):
            return f'[img]{github_raw_base}/{image_path}[/img]'
        return f'[img]{image_path}[/img]'
    
    content = re.sub(r'!\[(.*?)\]\((.*?)\)', replace_image, content)

    # Headers
    content = re.sub(r'^# (.*)$', r'[size=6][b]\1[/b][/size]', content, flags=re.MULTILINE)
    content = re.sub(r'^## (.*)$', r'[size=5][b]\1[/b][/size]', content, flags=re.MULTILINE)
    content = re.sub(r'^### (.*)$', r'[size=4][b]\1[/b][/size]', content, flags=re.MULTILINE)

    # Bold and Italic
    content = re.sub(r'\*\*([^*]+)\*\*', r'[b]\1[/b]', content)
    content = re.sub(r'\*([^*]+)\*', r'[i]\1[/i]', content)

    # Lists
    content = re.sub(r'^- (.*)$', r'[*] \1', content, flags=re.MULTILINE)
    content = re.sub(r'^[0-9]+\. (.*)$', r'[*] \1', content, flags=re.MULTILINE)

    # Links (process after images to avoid conflicts)
    content = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', r'[url=\2]\1[/url]', content)

    # Code blocks
    content = re.sub(r'```([^`]*)```', r'[code]\1[/code]', content, flags=re.DOTALL)
    content = re.sub(r'`([^`]*)`', r'[font=Courier New]\1[/font]', content)

    with open(output_file, 'w') as f:
        f.write(content)
    
    print(f"Successfully converted {input_file} to BBCode format at {output_file}")
    sys.exit(0)
except Exception as e:
    print(f"Error: {e}")
    sys.exit(1)
