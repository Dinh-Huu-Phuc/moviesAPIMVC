-- Chọn cơ sở dữ liệu để làm việc
USE MovieWebAPI;
GO

-- Xóa dữ liệu cũ (tùy chọn, để tránh trùng lặp khi chạy lại script)
DELETE FROM Movie_Actors;
DELETE FROM Movies;
DELETE FROM Studios;
DELETE FROM Actors;
GO

-- Đặt lại giá trị ID tự tăng (tùy chọn)
DBCC CHECKIDENT ('Movies', RESEED, 0);
DBCC CHECKIDENT ('Studios', RESEED, 0);
DBCC CHECKIDENT ('Actors', RESEED, 0);
GO

-- 1. Thêm dữ liệu cho bảng Studios (thay thế Publishers)
INSERT INTO Studios (Name)
VALUES
('Warner Bros. Pictures'),
('Paramount Pictures'),
('Universal Pictures'),
('Walt Disney Pictures'),
('20th Century Studios');
GO

-- 2. Thêm dữ liệu cho bảng Actors (thay thế Authors)
INSERT INTO Actors (FullName)
VALUES
('Tom Hanks'),
('Leonardo DiCaprio'),
('Scarlett Johansson'),
('Morgan Freeman'),
('Meryl Streep');
GO

-- 3. Thêm dữ liệu cho bảng Movies (thay thế Books)
-- Chú ý: IsWatched thay cho IsRead, DateWatched thay cho DateRead, Rating thay cho Rate, PosterUrl thay cho CoverUrl, StudioID thay cho PublisherID
INSERT INTO Movies (Title, Description, IsWatched, DateWatched, Rating, Genre, PosterUrl, DateAdded, StudioID)
VALUES
('Inception', 'A thief who steals corporate secrets through the use of dream-sharing technology.', 1, '2023-05-20', 5, 'Sci-Fi', 'https://placehold.co/300x450/000000/FFFFFF?text=Inception', '2023-01-10', 1),
('Forrest Gump', 'The presidencies of Kennedy and Johnson, the Vietnam War, the Watergate scandal and other historical events unfold.', 1, '2023-02-15', 5, 'Drama', 'https://placehold.co/300x450/000000/FFFFFF?text=Forrest+Gump', '2023-02-01', 2),
('Lost in Translation', 'A faded movie star and a neglected young woman form an unlikely bond after crossing paths in Tokyo.', 1, '2023-08-11', 4, 'Romance', 'https://placehold.co/300x450/000000/FFFFFF?text=Lost+in+Translation', '2023-03-12', 3),
('The Shawshank Redemption', 'Two imprisoned men bond over a number of years, finding solace and eventual redemption through acts of common decency.', 0, NULL, NULL, 'Drama', 'https://placehold.co/300x450/000000/FFFFFF?text=Shawshank', '2023-04-15', 1),
('The Devil Wears Prada', 'A smart but sensible new graduate lands a job as an assistant to Miranda Priestly, the demanding editor-in-chief of a high fashion magazine.', 1, '2024-01-05', 4, 'Comedy', 'https://placehold.co/300x450/000000/FFFFFF?text=Devil+Wears+Prada', '2023-05-22', 5);
GO

-- 4. Thêm dữ liệu cho bảng Movie_Actors (thay thế Book_Authors)
-- Liên kết các bộ phim với các diễn viên tương ứng
-- MovieId và ActorId sẽ tương ứng với thứ tự bạn đã INSERT ở trên (bắt đầu từ 1)
INSERT INTO Movie_Actors (MovieId, ActorId)
VALUES
(1, 2),  -- Inception - Leonardo DiCaprio
(2, 1),  -- Forrest Gump - Tom Hanks
(3, 3),  -- Lost in Translation - Scarlett Johansson
(4, 4),  -- The Shawshank Redemption - Morgan Freeman
(5, 5);  -- The Devil Wears Prada - Meryl Streep
GO

-- Thông báo hoàn tất
PRINT 'Sample data for Movie API has been successfully inserted.';
GO
