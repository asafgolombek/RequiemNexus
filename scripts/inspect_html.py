import urllib.request
import re

url = "https://codexofdarkness.com/wiki/Merits,_Universal_(2nd_Edition)"
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
try:
    with urllib.request.urlopen(req) as response:
        html = response.read().decode('utf-8')
        tables = re.findall(r'<table class=".*?wikitable.*?".*?>(.*?)</table>', html, re.DOTALL)
        for i, table in enumerate(tables):
            print(f"--- Table {i} ---")
            headers = re.findall(r'<th.*?>(.*?)</th>', table, re.DOTALL)
            print("Headers:", [re.sub(r'<[^>]+>', '', h).strip() for h in headers])
            rows = re.findall(r'<tr.*?>(.*?)</tr>', table, re.DOTALL)
            count = 0
            for row in rows:
                cols = re.findall(r'<td.*?>(.*?)</td>', row, re.DOTALL)
                if cols:
                    print("Row:", [re.sub(r'<[^>]+>', '', c).strip() for c in cols])
                    count += 1
                    if count >= 2:
                        break
except Exception as e:
    print("Error:", e)
