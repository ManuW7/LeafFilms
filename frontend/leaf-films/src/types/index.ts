export interface User {
  id: string
  username: string
  email: string
  role: 'user' | 'admin'
  createdAt: string
}

export interface AuthResponse {
  id: string
  username: string
  email: string
  token: string
}

export interface Movie {
  id: string
  title: string
  year: number
  genre: string
  director: string
  description?: string
  posterUrl?: string
  averageRating: number
  reviewCount: number
}

export interface Review {
  id: string
  userId: string
  username: string
  movieId: string
  movieTitle: string
  rating: number
  text: string
  likesCount: number
  createdAt: string
}

export interface Follow {
  id: string
  followerId: string
  followerName: string
  followedId: string
  followedName: string
  createdAt: string
}

export interface UserSummary {
  userId: string
  username: string
  followedAt: string
}

export interface WatchHistoryItem {
  id: string
  movieId: string
  movieTitle: string
  watchedAt: string
}

export interface WatchlistItem {
  id: string
  movieId: string
  movieTitle: string
  addedAt: string
}

export interface Playlist {
  id: string
  name: string
  createdAt: string
  movies: PlaylistMovie[]
}

export interface PlaylistMovie {
  movieId: string
  movieTitle: string
  addedAt: string
}

export interface FeedItem {
  id: string
  eventType: 'review_created' | 'movie_watched'
  actorId: string
  actorName: string
  movieId: string
  movieTitle: string
  extraText?: string
  rating?: number
  createdAt: string
}

export interface WsMessage {
  type: 'review_created' | 'movie_watched'
  actorName?: string
  movieTitle?: string
  rating?: number
  createdAt?: string
}
