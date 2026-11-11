#!/usr/bin/env python3
"""Regenerate config/levels_bundled.json and config/pool_bundled.json from the tarkov.dev item dump.

Usage:
  python tools/generate_bundles.py                  # download https://json.tarkov.dev/regular/items
  python tools/generate_bundles.py --input dump.json  # use an already-downloaded dump

Stdlib only. Reads the dump as data, nothing from it is executed.
"""
import argparse
import datetime
import re
import json
import os
import sys
import urllib.request
from collections import Counter

URL = "https://json.tarkov.dev/regular/items"
MONGO_ID = re.compile(r"[0-9a-fA-F]{24}")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONFIG = os.path.join(ROOT, "config")


def load(path):
    if path:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    req = urllib.request.Request(URL, headers={"User-Agent": "UltimateFlea-generator"})
    with urllib.request.urlopen(req, timeout=180) as r:
        return json.loads(r.read().decode("utf-8"))


def write(name, obj):
    with open(os.path.join(CONFIG, name), "w", encoding="utf-8", newline="\n") as f:
        json.dump(obj, f, indent=2, ensure_ascii=False)
        f.write("\n")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--input", help="path to an already-downloaded items dump")
    args = ap.parse_args()

    dump = load(args.input)
    items = dump["data"]["items"]
    today = datetime.date.today().isoformat()
    src = (f"generated from the tarkov.dev item dump ({URL}) on {today}, "
           f"{len(items)} items in the dump")

    levels, no_flea, skipped = {}, [], 0
    for tpl, it in items.items():
        if not isinstance(it, dict):
            continue
        # The feed carries a few placeholder keys (e.g. customdogtags12345678910).
        # SPT rejects anything that is not a 24-char hex MongoId, so drop them here.
        if not MONGO_ID.fullmatch(tpl):
            skipped += 1
            continue
        lvl = it.get("minLevelForFlea") or 0
        if isinstance(lvl, int) and lvl > 0:
            levels[tpl] = lvl
        if "noFlea" in (it.get("types") or []):
            no_flea.append(tpl)
    if skipped:
        print(f"skipped {skipped} entries with a non-MongoId key")
    levels = dict(sorted(levels.items()))
    no_flea.sort()

    write("levels_bundled.json", {
        "_comment": f"Per-item flea level requirement, field minLevelForFlea, {src}; {len(levels)} items with minLevelForFlea > 0 (0 = no extra requirement). Live Tarkov gates per item, so categories is empty. Regenerate with tools/generate_bundles.py.",
        "items": levels,
        "categories": {},
    })
    write("pool_bundled.json", {
        "_comment": f"Items that cannot be sold on the flea (types contains noFlea), {src}; {len(no_flea)} items. Used when pool.json useLiveBlacklist is true. Regenerate with tools/generate_bundles.py.",
        "noFlea": no_flea,
    })

    print(f"items in dump: {len(items)}")
    print(f"levels_bundled.json: {len(levels)} items")
    for lvl, n in sorted(Counter(levels.values()).items()):
        print(f"  level {lvl}: {n}")
    print(f"pool_bundled.json: {len(no_flea)} noFlea items")


if __name__ == "__main__":
    sys.exit(main())
