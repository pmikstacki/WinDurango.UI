import json
import os

f = open(os.path.join(os.path.dirname(__file__), "..", "Assets", "contributors.txt"), "w+")

try:
    import requests
except ImportError:
    # didn't even know you could do this
    os.system("pip install requests")
    import requests
except:
    print("Requests is missing and the script couldn't install it...")
    exit(0)

try:
    contribs = requests.get("https://api.github.com/repos/WinDurango/WinDurango.UI/contributors?anon=1&per_page=50")

    for contributor in contribs.json():
        # why tf did they call it login
        name = contributor.get("login", None)
        pfp = contributor.get("avatar_url", None)
        url = contributor.get("html_url", None)
        contribution_count = str(contributor.get("contributions", None))
        f.write(name.replace(";", "WD_CONTRIB_SEMICOLON") + ";" + pfp.replace(";", "WD_CONTRIB_SEMICOLON") + ";" + url.replace(";", "WD_CONTRIB_SEMICOLON") + ";" + contribution_count + "\n")
except:
    print("Couldn't fetch contributor information.")
    exit(0)



f.close()


