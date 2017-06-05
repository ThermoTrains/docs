background = imread('../../../samples/motioncrop/background.jpg');
frame1 = imread('../../../samples/motioncrop/train-1.jpg');
frame2 = imread('../../../samples/motioncrop/train-2.jpg');

background = rgb2gray(background);
frame1 = rgb2gray(frame1);
frame2 = rgb2gray(frame2);

background = imgaussfilt(background, 5, 'FilterSize', [21 21]);
frame1 = imgaussfilt(frame1, 5, 'FilterSize', [21 21]);

diff = background - frame1;
level = graythresh(diff);
BW = imbinarize(diff,level);

se = strel('rectangle',[40 40]);
BW = imdilate(BW,se);

[B,L] = bwboundaries(BW,'noholes');
imshow(label2rgb(L, @jet, [.5 .5 .5]))
hold on
for k = 1:length(B)
   boundary = B{k};
   maxX = max(boundary(:,1));
   minX = min(boundary(:,1));
   maxY = max(boundary(:,2));
   minY = min(boundary(:,2));
   
   plot(boundary(:,2), boundary(:,1), 'w', 'LineWidth', 2)
end

imshow(BW);
figure;
imcontour(BW);
