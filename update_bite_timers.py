import os
import requests
import json

def fetch_bite_data():
    url = "https://gubal.ffxivteamcraft.com/graphql"
    query = """
    query BiteTimesPerFishPerSpotQuery($spotId: Int) {
        biteTimes: bite_time_per_fish_per_spot(
            where: {
                spot: {_eq: $spotId}, 
                flooredBiteTime: {_gt: 1, _lt: 600}, 
                occurences: {_gte: 3}
            }
        ) {
            itemId
            spot
            flooredBiteTime
            occurences
        }
    }
    """
    
    payload = {
        "operationName": "BiteTimesPerFishPerSpotQuery",
        "variables": {},
        "query": query
    }
    
    response = requests.post(
        url, 
        json=payload, 
        headers={"Content-Type": "application/json"}
    )
    
    if response.status_code != 200:
        raise RuntimeError(f"API request failed: {response.text}")
        
    return response.json()

def calculate_quantile(data_points, target_quantile):
    if not data_points:
        return None
        
    total_count = sum(count for _, count, _ in data_points)
    cumulative_count = 0
    
    for value, count, _ in data_points:
        cumulative_count += count
        if cumulative_count > total_count * target_quantile:
            return value
    return None

def calculate_statistics(bite_time_series):
    first_quartile = calculate_quantile(bite_time_series, 0.25)
    third_quartile = calculate_quantile(bite_time_series, 0.75)
    lower_percentile = calculate_quantile(bite_time_series, 0.02)
    upper_percentile = calculate_quantile(bite_time_series, 0.98)
    
    bite_times = [time for time, _, _ in bite_time_series]
    counts = [count for _, count, _ in bite_time_series]
    
    min_time = min(bite_times)
    max_time = max(bite_times)
    median = calculate_quantile(bite_time_series, 0.5)
    
    total_weighted_time = sum(time * count for time, count, _ in bite_time_series)
    total_count = sum(counts)
    mean = total_weighted_time / total_count
    
    iqr = third_quartile - first_quartile
    whisker_min = max(first_quartile - 1.5 * iqr, min_time)
    whisker_max = min(third_quartile + 1.5 * iqr, max_time)
    
    return {
        "itemId": bite_time_series[0][2],
        "min": min_time,
        "median": median,
        "mean": mean,
        "max": max_time,
        "whiskerMin": whisker_min,
        "whiskerMax": whisker_max,
        "q1": lower_percentile,
        "q3": upper_percentile
    }

def process_bite_data(bite_data):
    fish_ids = set(entry['itemId'] for entry in bite_data['data']['biteTimes'])
    
    bite_time_series = []
    for fish_id in fish_ids:
        fish_bites = sorted(
            [(entry['flooredBiteTime'], entry['occurences'], entry['itemId']) 
             for entry in bite_data['data']['biteTimes'] 
             if entry['itemId'] == fish_id],
            key=lambda x: x[0]
        )
        
        if fish_bites:
            bite_time_series.append(fish_bites)
    
    return [calculate_statistics(series) for series in bite_time_series]

def main():
    try:
        bite_data = fetch_bite_data()
        
        statistics = process_bite_data(bite_data)
        
        statistics = sorted(statistics, key=lambda x: x['itemId'])
        
        output_dir = os.path.join('AutoHook', 'Data', 'FishData')
        os.makedirs(output_dir, exist_ok=True)
        
        output_path = os.path.join(output_dir, 'bitetimers.json')
        
        # Write the sorted statistics to the JSON file
        with open(output_path, 'w', encoding='utf-8') as file:
            json.dump(statistics, file, indent=2)
        
        print("Successfully updated bitetimers.json")
        
    except Exception as error:
        print(f"Error: {error}")
        raise

if __name__ == "__main__":
    main()
