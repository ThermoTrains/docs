function rectify() 
    clear all; close all; clc; %clear matrices, close figures & clear cmd wnd.

    I = imread('../../../samples/rectify/train-1.jpg');
    Igauss = imgaussfilt(I,4);
    H = rgb2hsv(Igauss);

    VALUE = H(:,:,3);
    height = double(int16(size(VALUE,1)/3));

    upperPart = VALUE(1:height,:,:);
    lowerPart = VALUE(height*2:size(VALUE,1)-1,:,:);
    
    lines = zeros(size(I(:,:,1)));
    
    lines = findMaxFrequency(upperPart, lines, 0);
    lines = findMaxFrequency(lowerPart, lines, height*2);

    [H,T,R] = hough(lines);
    P  = houghpeaks(H,5,'threshold',ceil(0.3*max(H(:))));
    lines = houghlines(lines,T,R,P,'FillGap',50,'MinLength',300);
    figure, imshow(I), hold on
    max_len = 0;
    for k = 1:length(lines)
       xy = [lines(k).point1; lines(k).point2];
       plot(xy(:,1),xy(:,2),'LineWidth',2,'Color','green');

       % Plot beginnings and ends of lines
       plot(xy(1,1),xy(1,2),'x','LineWidth',2,'Color','yellow');
       plot(xy(2,1),xy(2,2),'x','LineWidth',2,'Color','red');

       % Determine the endpoints of the longest line segment
       len = norm(lines(k).point1 - lines(k).point2);
       if ( len > max_len)
          max_len = len;
       end
    end
    
    function [output] = findMaxFrequency(im, output, xOffset)

        gheight = size(im,1);
        gwidth = size(im,2);
 
        maxvals = zeros(gwidth,1);

        for x = 1:gwidth
            hist = zeros(gheight-10,1);
            top = im(1:gheight-1,x);
            bottom = im(2:gheight,x);
                
            for i = 10:gheight-1
                diff = top(i-9:i)-bottom(i-9:i);
                hist(i) = mean(diff);
            end
            [~, idx] = max(hist);
            maxvals(x) = idx;
        end
        
        for i = 1:gwidth
            output(maxvals(i)+xOffset,i,1) = 255;
        end
    end
end