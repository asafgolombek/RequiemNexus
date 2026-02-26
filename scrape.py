import urllib.request
import json
from bs4 import BeautifulSoup
import re

def get_soup(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    html = urllib.request.urlopen(req).read()
    return BeautifulSoup(html, 'html.parser')

data = {'disciplines': [], 'merits': []}

def scrape_disciplines():
    soup = get_soup('https://codexofdarkness.com/wiki/Disciplines,_Core_(2nd_Edition)')
    
    # Try finding headers (Usually h2 or h3 with the discipline name)
    # The structure might be: hx -> a few elements -> table
    for h in soup.find_all(['h2', 'h3']):
        disc_name = h.text.strip().replace('[edit]', '').replace(' (2nd Edition)', '').replace(' (Vampire)', '')
        
        # We only care about main disciplines (Animalism, Auspex, Celerity, Dominate, Majesty, Nightmare, Obfuscate, Protean, Resilience, Vigor)
        core_names = ['Animalism', 'Auspex', 'Celerity', 'Dominate', 'Majesty', 'Nightmare', 'Obfuscate', 'Protean', 'Resilience', 'Vigor']
        if disc_name not in core_names:
            continue
            
        # Find next table
        table = h.find_next('table')
        if not table: continue
        
        powers = []
        rows = table.find_all('tr')
        if not rows: continue
        
        # check if it's the right table
        headers = [th.text.strip() for th in rows[0].find_all(['th', 'td'])]
        
        book_idx = -1
        desc_idx = -1
        dice_idx = -1
        
        for i, hh in enumerate(headers):
            if 'Book' in hh or 'Reference' in hh or 'Corebook' in hh or '2nd Edition' in hh:
                desc_idx = i
            elif 'Dice' in hh or 'Pool' in hh:
                dice_idx = i
            elif 'Description' in hh or 'Effect' in hh:
                desc_idx = i
        
        for tr in rows[1:]:
            tds = tr.find_all(['td', 'th'])
            if len(tds) >= 3:
                # Name is usually col 0 or 1
                name = tds[0].text.strip()
                # Dots might be in col 0 with name, or col 1
                dots = tds[1].text.strip() if len(tds) > 1 else ""
                level = dots.count('•') + dots.count('*') + dots.count('●')
                if level == 0:
                    try:
                        level = int(re.search(r'\d+', dots).group())
                    except:
                        level = 0
                
                desc = tds[desc_idx].text.strip() if desc_idx != -1 and desc_idx < len(tds) else ""
                
                # If there's no dedicated description col, it might be the last one
                if desc == "" and len(tds) > 2:
                    desc = tds[2].text.strip()

                dice = tds[dice_idx].text.strip() if dice_idx != -1 and dice_idx < len(tds) else ""
                if dice == "" and len(tds) > 3:
                     # try to grab the last column as dice pool if we didn't find desc
                     dice = tds[-1].text.strip()
                     
                powers.append({'name': name, 'level': level, 'desc': desc, 'dice': dice})
        
        if powers:
            data['disciplines'].append({'name': disc_name, 'powers': powers})

def scrape_merits():
    soup = get_soup('https://codexofdarkness.com/wiki/Merits,_Vampire_(2nd_Edition)')
    tables = soup.find_all('table')
    
    for t in tables:
        rows = t.find_all('tr')
        if not rows: continue
        
        headers = [th.text.strip().lower() for th in rows[0].find_all(['th', 'td'])]
        
        # Needs to look like a merit table
        book_idx = -1
        name_idx = -1
        rating_idx = -1
        desc_idx = -1
        
        for i, h in enumerate(headers):
            if 'book' in h or 'reference' in h: book_idx = i
            elif 'name' in h or 'merit' in h: name_idx = i
            elif 'cost' in h or 'rating' in h or 'dots' in h: rating_idx = i
            elif 'description' in h or 'effect' in h: desc_idx = i
            
        if book_idx == -1: 
            # If no book column, it might have it in the third col
            book_idx = 3 if len(headers) > 3 else -1
            
        if name_idx == -1: name_idx = 0
        if rating_idx == -1: rating_idx = 1
        if desc_idx == -1: desc_idx = 2
            
        for tr in rows[1:]:
            tds = tr.find_all('td')
            if not tds or len(tds) <= book_idx: continue
            
            book = tds[book_idx].text.strip()
            if 'vtr 2e' in book.lower() or 'vtr2e' in book.lower() or 'requiem 2' in book.lower() or 'corebook' in book.lower() or 'second edition' in book.lower():
                name = tds[name_idx].text.strip() if name_idx < len(tds) else ''
                rating = tds[rating_idx].text.strip() if rating_idx < len(tds) else ''
                desc = tds[desc_idx].text.strip() if desc_idx < len(tds) else ''
                
                # filter out weird rows
                if name and len(name) < 50:
                    data['merits'].append({'name': name, 'rating': rating, 'desc': desc})

try:
    scrape_disciplines()
except Exception as e:
    print(f"Error scraping disciplines: {e}")

try:
    scrape_merits()
except Exception as e:
    print(f"Error scraping merits: {e}")

with open('scraped_data.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2)
print(f"Scraping complete. Found {len(data['disciplines'])} disciplines and {len(data['merits'])} merits.")
