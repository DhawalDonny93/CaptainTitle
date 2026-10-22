"""Tiny helper used for the one-off AI-SEO removal: delete C# members (methods/properties/classes) by signature regex.
Brace-matches from the first '{' after the signature (or ends at ';' for expression/auto members) and also removes the
member's leading comment / attribute lines."""
import re, sys

def remove_member(text, sig_regex, count=1, must=True):
    lines = text.split('\n')
    removed = 0
    i = 0
    while i < len(lines) and removed < count:
        if re.search(sig_regex, lines[i]):
            start = i
            while start > 0 and re.match(r'\s*(///|//|\[)', lines[start-1]): start -= 1
            # find end
            j = i; depth = 0; seen = False
            while j < len(lines):
                l = lines[j]
                # strip string/char literals crudely
                l2 = re.sub(r'"(?:\\.|[^"\\])*"', '""', l)
                l2 = re.sub(r"'(?:\\.|[^'\\])'", "''", l2)
                l2 = re.sub(r'//.*$', '', l2)
                for ch in l2:
                    if ch == '{': depth += 1; seen = True
                    elif ch == '}': depth -= 1
                if seen and depth == 0: break
                if not seen and l2.rstrip().endswith(';'): break
                j += 1
            del lines[start:j+1]
            # collapse doubled blank line
            if start < len(lines) and start > 0 and lines[start].strip() == '' and lines[start-1].strip() == '':
                del lines[start]
            removed += 1
            i = start
        else:
            i += 1
    if must and removed < count: raise SystemExit(f'signature not found: {sig_regex}')
    return '\n'.join(lines)
