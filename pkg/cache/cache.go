package cache

import (
	"encoding/gob"
	"fmt"
	"os"
	"os/exec"
	"runtime"
	"strconv"
	"strings"
	"time"

	"git-metrics/pkg/models"
	"git-metrics/pkg/utils"
)

// ObjectCache stores metadata for all Git objects with timestamps
type ObjectCache struct {
	Version       string
	BuildTime     time.Time
	RepositoryURL string
	Objects       []CachedObject
}

// CachedObject represents a Git object with its metadata
type CachedObject struct {
	ObjectID         string
	ObjectType       string // "commit", "tree", "blob"
	Path             string // File path (for blobs)
	UncompressedSize int64
	CompressedSize   int64
	FirstSeenDate    time.Time // When this object was first introduced
}

// BuildCache collects all Git objects and their metadata
func BuildCache(repositoryPath string, debug bool) (*ObjectCache, error) {
	utils.DebugPrint(debug, "Building object cache...")
	startTime := time.Now()

	cache := &ObjectCache{
		Version:   "1.0",
		BuildTime: startTime,
		Objects:   make([]CachedObject, 0),
	}

	// Get repository remote URL
	cmd := exec.Command("git", "-C", repositoryPath, "remote", "get-url", "origin")
	if output, err := cmd.Output(); err == nil {
		cache.RepositoryURL = strings.TrimSpace(string(output))
	}

	// Step 1: Get all commits with their timestamps
	utils.DebugPrint(debug, "Collecting commit timestamps...")
	commitTimestamps, err := getCommitTimestamps(repositoryPath, debug)
	if err != nil {
		return nil, fmt.Errorf("failed to get commit timestamps: %w", err)
	}
	utils.DebugPrint(debug, "Found %d commits", len(commitTimestamps))

	// Step 2: Get all objects with sizes
	utils.DebugPrint(debug, "Collecting all objects with sizes...")
	objectMetadata, err := getAllObjectsWithSizes(repositoryPath, debug)
	if err != nil {
		return nil, fmt.Errorf("failed to get object metadata: %w", err)
	}
	utils.DebugPrint(debug, "Found %d objects", len(objectMetadata))

	// Step 3: Find first-seen date for each object
	utils.DebugPrint(debug, "Finding first-seen dates for objects...")
	objectFirstSeen, err := getObjectFirstSeenDates(repositoryPath, commitTimestamps, debug)
	if err != nil {
		return nil, fmt.Errorf("failed to get first-seen dates: %w", err)
	}
	utils.DebugPrint(debug, "Mapped %d objects to first-seen dates", len(objectFirstSeen))

	// Step 4: Combine data into cache objects
	utils.DebugPrint(debug, "Building cache entries...")
	for objectID, metadata := range objectMetadata {
		firstSeen := objectFirstSeen[objectID]
		if firstSeen.IsZero() {
			// Object not found in any commit, skip it
			continue
		}

		cache.Objects = append(cache.Objects, CachedObject{
			ObjectID:         objectID,
			ObjectType:       metadata.ObjectType,
			Path:             metadata.Path,
			UncompressedSize: metadata.UncompressedSize,
			CompressedSize:   metadata.CompressedSize,
			FirstSeenDate:    firstSeen,
		})
	}

	utils.DebugPrint(debug, "Cache built with %d objects in %v", len(cache.Objects), time.Since(startTime))
	return cache, nil
}

// SaveCache saves the cache to a file
func SaveCache(cache *ObjectCache, filename string, debug bool) error {
	utils.DebugPrint(debug, "Saving cache to %s...", filename)

	file, err := os.Create(filename)
	if err != nil {
		return fmt.Errorf("failed to create cache file: %w", err)
	}
	defer file.Close()

	encoder := gob.NewEncoder(file)
	if err := encoder.Encode(cache); err != nil {
		return fmt.Errorf("failed to encode cache: %w", err)
	}

	fileInfo, _ := file.Stat()
	utils.DebugPrint(debug, "Cache saved (%s)", utils.FormatSize(fileInfo.Size()))
	return nil
}

// LoadCache loads the cache from a file
func LoadCache(filename string, debug bool) (*ObjectCache, error) {
	utils.DebugPrint(debug, "Loading cache from %s...", filename)

	file, err := os.Open(filename)
	if err != nil {
		return nil, fmt.Errorf("failed to open cache file: %w", err)
	}
	defer file.Close()

	var cache ObjectCache
	decoder := gob.NewDecoder(file)
	if err := decoder.Decode(&cache); err != nil {
		return nil, fmt.Errorf("failed to decode cache: %w", err)
	}

	utils.DebugPrint(debug, "Cache loaded with %d objects (built %s)",
		len(cache.Objects), cache.BuildTime.Format("2006-01-02 15:04:05"))
	return &cache, nil
}

// QueryDateRange returns statistics for objects within a date range
func (cache *ObjectCache) QueryDateRange(startDate, endDate time.Time, debug bool) models.GrowthStatistics {
	utils.DebugPrint(debug, "Querying cache for range %s to %s",
		startDate.Format("2006-01-02"), endDate.Format("2006-01-02"))

	stats := models.GrowthStatistics{
		Year: startDate.Year(),
	}

	// Normalize dates to start/end of day
	startDate = time.Date(startDate.Year(), startDate.Month(), startDate.Day(), 0, 0, 0, 0, time.UTC)
	endDate = time.Date(endDate.Year(), endDate.Month(), endDate.Day(), 23, 59, 59, 999999999, time.UTC)

	var commits, trees, blobs int
	var compressed, uncompressed int64
	blobsMap := make(map[string]models.FileInformation)

	for _, obj := range cache.Objects {
		// Check if object was first seen within the date range
		if obj.FirstSeenDate.After(startDate) && obj.FirstSeenDate.Before(endDate) ||
		   obj.FirstSeenDate.Equal(startDate) || obj.FirstSeenDate.Equal(endDate) {

			compressed += obj.CompressedSize
			uncompressed += obj.UncompressedSize

			switch obj.ObjectType {
			case "commit":
				commits++
			case "tree":
				trees++
			case "blob":
				blobs++
				if obj.Path != "" {
					if existing, ok := blobsMap[obj.Path]; ok {
						existing.Blobs++
						existing.CompressedSize += obj.CompressedSize
						existing.UncompressedSize += obj.UncompressedSize
						blobsMap[obj.Path] = existing
					} else {
						blobsMap[obj.Path] = models.FileInformation{
							Path:             obj.Path,
							Blobs:            1,
							CompressedSize:   obj.CompressedSize,
							UncompressedSize: obj.UncompressedSize,
						}
					}
				}
			}
		}
	}

	stats.Commits = commits
	stats.Trees = trees
	stats.Blobs = blobs
	stats.Compressed = compressed
	stats.Uncompressed = uncompressed

	// Convert blobs map to slice
	for _, fileInfo := range blobsMap {
		stats.LargestFiles = append(stats.LargestFiles, fileInfo)
	}

	utils.DebugPrint(debug, "Found %d commits, %d trees, %d blobs in date range", commits, trees, blobs)
	return stats
}

// Helper functions

type objectMetadata struct {
	ObjectType       string
	Path             string
	UncompressedSize int64
	CompressedSize   int64
}

func shellToUse() string {
	if runtime.GOOS == "windows" {
		return "bash"
	}
	return "sh"
}

func getCommitTimestamps(repositoryPath string, debug bool) (map[string]time.Time, error) {
	cmd := exec.Command("git", "-C", repositoryPath, "log", "--all", "--pretty=format:%H %ct")
	output, err := cmd.Output()
	if err != nil {
		return nil, err
	}

	timestamps := make(map[string]time.Time)
	lines := strings.Split(string(output), "\n")
	for _, line := range lines {
		if strings.TrimSpace(line) == "" {
			continue
		}
		fields := strings.Fields(line)
		if len(fields) < 2 {
			continue
		}

		commitHash := fields[0]
		timestamp, err := strconv.ParseInt(fields[1], 10, 64)
		if err != nil {
			continue
		}

		timestamps[commitHash] = time.Unix(timestamp, 0)
	}

	return timestamps, nil
}

func getAllObjectsWithSizes(repositoryPath string, debug bool) (map[string]objectMetadata, error) {
	commandString := "git rev-list --objects --all | git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(objectsize:disk) %(rest)'"
	cmd := exec.Command(shellToUse(), "-c", fmt.Sprintf("cd %s && %s", repositoryPath, commandString))
	output, err := cmd.Output()
	if err != nil {
		return nil, err
	}

	metadata := make(map[string]objectMetadata)
	lines := strings.Split(string(output), "\n")

	for _, line := range lines {
		if strings.TrimSpace(line) == "" {
			continue
		}
		fields := strings.Fields(line)
		if len(fields) < 4 {
			continue
		}

		objectType := fields[0]
		objectID := fields[1]
		uncompressedSize, _ := strconv.ParseInt(fields[2], 10, 64)
		compressedSize, _ := strconv.ParseInt(fields[3], 10, 64)

		path := ""
		if len(fields) >= 5 {
			path = strings.Join(fields[4:], " ")
		}

		metadata[objectID] = objectMetadata{
			ObjectType:       objectType,
			Path:             path,
			UncompressedSize: uncompressedSize,
			CompressedSize:   compressedSize,
		}
	}

	return metadata, nil
}

func getObjectFirstSeenDates(repositoryPath string, commitTimestamps map[string]time.Time, debug bool) (map[string]time.Time, error) {
	// This is the expensive part - we need to walk through all commits in chronological order
	// and track when each object first appears

	utils.DebugPrint(debug, "Walking commit history to find first-seen dates...")

	// Get commits in reverse chronological order (oldest first)
	cmd := exec.Command("git", "-C", repositoryPath, "rev-list", "--all", "--reverse")
	output, err := cmd.Output()
	if err != nil {
		return nil, err
	}

	firstSeen := make(map[string]time.Time)
	commits := strings.Split(strings.TrimSpace(string(output)), "\n")

	utils.DebugPrint(debug, "Processing %d commits to find object first-seen dates...", len(commits))

	for i, commitHash := range commits {
		if strings.TrimSpace(commitHash) == "" {
			continue
		}

		if debug && i > 0 && i % 1000 == 0 {
			utils.DebugPrint(debug, "Processed %d/%d commits...", i, len(commits))
		}

		commitTime := commitTimestamps[commitHash]

		// Record the commit itself
		if _, exists := firstSeen[commitHash]; !exists {
			firstSeen[commitHash] = commitTime
		}

		// Get all objects reachable from this commit
		cmd := exec.Command("git", "-C", repositoryPath, "rev-list", "--objects", commitHash, "--not", "--all", fmt.Sprintf("^%s^", commitHash))
		output, err := cmd.Output()
		if err != nil {
			// For the first commit, this will fail, so just get all objects from this commit
			cmd = exec.Command("git", "-C", repositoryPath, "rev-list", "--objects", commitHash)
			output, _ = cmd.Output()
		}

		lines := strings.Split(string(output), "\n")
		for _, line := range lines {
			if strings.TrimSpace(line) == "" {
				continue
			}
			fields := strings.Fields(line)
			if len(fields) == 0 {
				continue
			}

			objectID := fields[0]
			if _, exists := firstSeen[objectID]; !exists {
				firstSeen[objectID] = commitTime
			}
		}
	}

	utils.DebugPrint(debug, "First-seen dates determined for %d objects", len(firstSeen))
	return firstSeen, nil
}
