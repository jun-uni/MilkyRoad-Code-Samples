import json
from pathlib import Path

import pandas as pd


INPUT_DIRECTORY = Path("DataSheet")
OUTPUT_DIRECTORY = Path("json_output")


def convert_csv_files() -> None:
    csv_files = sorted(
        path for path in INPUT_DIRECTORY.iterdir()
        if path.is_file() and path.suffix.lower() == ".csv"
    )

    if not csv_files:
        raise FileNotFoundError("DataSheet 폴더에서 CSV 파일을 찾을 수 없습니다.")

    OUTPUT_DIRECTORY.mkdir(parents=True, exist_ok=True)

    for csv_path in csv_files:
        frame = pd.read_csv(csv_path, encoding="utf-8")
        frame = frame.astype(object).where(frame.notna(), None)
        records = frame.to_dict(orient="index")
        output_path = OUTPUT_DIRECTORY / f"{csv_path.stem}.json"

        with output_path.open("w", encoding="utf-8") as output_file:
            json.dump(
                records,
                output_file,
                indent=2,
                ensure_ascii=False,
                allow_nan=False,
            )


if __name__ == "__main__":
    convert_csv_files()
